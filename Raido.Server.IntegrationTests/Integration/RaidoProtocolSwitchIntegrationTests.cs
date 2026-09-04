using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Raido.Common.Protocol;
using Raido.Server.IntegrationTests.Infrastructure;

namespace Raido.Server.IntegrationTests.Integration;

[TestClass]
[DoNotParallelize]
public sealed class RaidoProtocolSwitchIntegrationTests
{
    private static readonly TimeSpan ObservationTimeout = TimeSpan.FromSeconds(10);

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketReplacementPreservesSwitchedProtocol()
    {
        await using var server = new RaidoTestServer();
        await server.StartAsync();
        await using var clientA = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync().WaitAsync(ObservationTimeout);
        var logical = server.LogicalConnection;
        var connectionId = logical.ConnectionId;
        await server.Application.DispatcherConnected.Task.WaitAsync(ObservationTimeout);

        server.Application.HoldDispatch = true;
        await clientA.SendAsync(RaidoTestProtocol.Encode(0x61));
        await server.Application.WaitForMessageAsync(0x61).WaitAsync(ObservationTimeout);
        await server.Application.DispatchEntered.Task.WaitAsync(ObservationTimeout);

        var protocolB = new RaidoAlternateTestProtocol();
        await logical.SetProtocolAsync(protocolB, CancellationToken.None);
        server.Application.ReleaseDispatch.TrySetResult();

        await clientA.SendAsync(RaidoAlternateTestProtocol.Encode(0x62));
        await server.Application.WaitForMessageAsync(0x62).WaitAsync(ObservationTimeout);
        await logical.WriteAsync(new RaidoTestMessage(0x63));
        using (var readTimeout = new CancellationTokenSource(ObservationTimeout))
        {
            CollectionAssert.AreEqual(
                RaidoAlternateTestProtocol.Encode(0x63),
                await clientA.ReadFrameAsync(readTimeout.Token));
        }

        await clientA.DisposeAsync();
        await server.WaitForInitialPhysicalCloseAsync().WaitAsync(ObservationTimeout);

        await using var clientB = await server.ConnectClientAsync();
        var replacement = await server.AcceptReplacementAsync().WaitAsync(ObservationTimeout);
        Assert.IsTrue(server.ActivateReplacement(replacement));

        await clientB.SendAsync(RaidoAlternateTestProtocol.Encode(0x64));
        await server.Application.WaitForMessageAsync(0x64).WaitAsync(ObservationTimeout);
        await logical.WriteAsync(new RaidoTestMessage(0x65));
        using (var readTimeout = new CancellationTokenSource(ObservationTimeout))
        {
            CollectionAssert.AreEqual(
                RaidoAlternateTestProtocol.Encode(0x65),
                await clientB.ReadFrameAsync(readTimeout.Token));
        }

        Assert.AreSame(logical, server.LogicalConnection);
        Assert.AreEqual(connectionId, logical.ConnectionId);
        Assert.AreSame(protocolB, logical.Protocol);
        Assert.AreEqual(1, server.Application.DispatcherConnectedCount);
        Assert.AreEqual(1, server.Application.LifetimeConnectedCount);
        Assert.AreEqual(0, server.Application.DispatcherDisconnectedCount);
        Assert.AreEqual(0, server.Application.LifetimeDisconnectedCount);

        logical.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(ObservationTimeout);
        await server.Application.LifetimeDisconnected.Task.WaitAsync(ObservationTimeout);
        Assert.AreEqual(1, server.Application.DispatcherDisconnectedCount);
        Assert.AreEqual(1, server.Application.LifetimeDisconnectedCount);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketBufferedFrameUsesProtocolSelectedByPreviousDispatch()
    {
        await using var server = new RaidoTestServer();
        await server.StartAsync();
        await using var client = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync().WaitAsync(ObservationTimeout);
        var logical = server.LogicalConnection;
        await server.Application.DispatcherConnected.Task.WaitAsync(ObservationTimeout);

        var protocolB = new RaidoAlternateTestProtocol();
        server.Application.DispatchMessageCallback = (connection, message) =>
            message is RaidoTestMessage { Id: RaidoTestProtocol.BufferedFrameFirstMessageId }
                ? connection.SetProtocolAsync(protocolB, CancellationToken.None).AsTask()
                : Task.CompletedTask;

        // This single send must be buffered in full before A dispatches; the A parser deliberately waits for both
        // frames so B remains in the same RaidoProtocolReader buffer when the dispatch callback selects Protocol B.
        var combinedFrames = new byte[RaidoTestProtocol.FrameSize * 2];
        Buffer.BlockCopy(
            RaidoTestProtocol.Encode(RaidoTestProtocol.BufferedFrameFirstMessageId),
            0,
            combinedFrames,
            0,
            RaidoTestProtocol.FrameSize);
        Buffer.BlockCopy(
            RaidoAlternateTestProtocol.Encode(0x72),
            0,
            combinedFrames,
            RaidoTestProtocol.FrameSize,
            RaidoTestProtocol.FrameSize);
        await client.SendAsync(combinedFrames);

        await server.Application.WaitForMessageAsync(RaidoTestProtocol.BufferedFrameFirstMessageId).WaitAsync(ObservationTimeout);
        await server.Application.WaitForMessageAsync(0x72).WaitAsync(ObservationTimeout);

        CollectionAssert.AreEqual(
            new[] { (int)RaidoTestProtocol.BufferedFrameFirstMessageId, 0x72 },
            server.Application.ReceivedMessageIds.ToArray());
        Assert.AreSame(protocolB, logical.Protocol);
        Assert.AreEqual(1, server.Application.DispatcherConnectedCount);
        Assert.AreEqual(1, server.Application.LifetimeConnectedCount);
        Assert.AreEqual(0, server.Application.DispatcherDisconnectedCount);
        Assert.AreEqual(0, server.Application.LifetimeDisconnectedCount);

        logical.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(ObservationTimeout);
        await server.Application.LifetimeDisconnected.Task.WaitAsync(ObservationTimeout);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task OwnedProtocolLifetimeSurvivesPhysicalReplacementUntilLogicalCleanup()
    {
        await using var server = new RaidoTestServer();
        await server.StartAsync();
        await using var clientA = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync().WaitAsync(ObservationTimeout);
        var logical = server.LogicalConnection;
        var connectionId = logical.ConnectionId;
        await server.Application.DispatcherConnected.Task.WaitAsync(ObservationTimeout);

        server.Application.HoldDispatch = true;
        await clientA.SendAsync(RaidoTestProtocol.Encode(0x81));
        await server.Application.WaitForMessageAsync(0x81).WaitAsync(ObservationTimeout);
        await server.Application.DispatchEntered.Task.WaitAsync(ObservationTimeout);

        var protocolB = new RaidoAlternateTestProtocol();
        var protocolLifetime = new TrackingProtocolLifetime();
        await logical.SetProtocolAsync(protocolB, protocolLifetime, CancellationToken.None);
        Assert.AreEqual(0, protocolLifetime.DisposeCount);
        server.Application.ReleaseDispatch.TrySetResult();

        await clientA.DisposeAsync();
        await server.WaitForInitialPhysicalCloseAsync().WaitAsync(ObservationTimeout);
        Assert.AreEqual(0, protocolLifetime.DisposeCount);
        Assert.AreEqual(0, server.Application.DispatcherDisconnectedCount);
        Assert.AreEqual(0, server.Application.LifetimeDisconnectedCount);

        await using var clientB = await server.ConnectClientAsync();
        var replacement = await server.AcceptReplacementAsync().WaitAsync(ObservationTimeout);
        Assert.IsTrue(server.ActivateReplacement(replacement));

        await clientB.SendAsync(RaidoAlternateTestProtocol.Encode(0x82));
        await server.Application.WaitForMessageAsync(0x82).WaitAsync(ObservationTimeout);
        Assert.AreEqual(0, protocolLifetime.DisposeCount);
        Assert.AreSame(logical, server.LogicalConnection);
        Assert.AreEqual(connectionId, logical.ConnectionId);
        Assert.AreSame(protocolB, logical.Protocol);

        logical.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(ObservationTimeout);
        await server.Application.LifetimeDisconnected.Task.WaitAsync(ObservationTimeout);
        Assert.AreEqual(1, protocolLifetime.DisposeCount);
        Assert.AreEqual(1, server.Application.DispatcherDisconnectedCount);
        Assert.AreEqual(1, server.Application.LifetimeDisconnectedCount);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketProtocolSwitchUsesNewProtocolForSubsequentInboundAndOutboundMessages()
    {
        await using var server = new RaidoTestServer();
        await server.StartAsync();
        await using var client = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync().WaitAsync(ObservationTimeout);
        var logical = server.LogicalConnection;
        await server.Application.DispatcherConnected.Task.WaitAsync(ObservationTimeout);

        server.Application.HoldDispatch = true;
        await client.SendAsync(RaidoTestProtocol.Encode(0x11));
        await server.Application.WaitForMessageAsync(0x11).WaitAsync(ObservationTimeout);
        await server.Application.DispatchEntered.Task.WaitAsync(ObservationTimeout);

        var protocolB = new RaidoAlternateTestProtocol();
        await logical.SetProtocolAsync(protocolB, CancellationToken.None);
        server.Application.ReleaseDispatch.TrySetResult();

        await client.SendAsync(RaidoAlternateTestProtocol.Encode(0x22));
        await server.Application.WaitForMessageAsync(0x22).WaitAsync(ObservationTimeout);

        await logical.WriteAsync(new RaidoTestMessage(0x33));
        using (var readTimeout = new CancellationTokenSource(ObservationTimeout))
        {
            CollectionAssert.AreEqual(
                RaidoAlternateTestProtocol.Encode(0x33),
                await client.ReadFrameAsync(readTimeout.Token));
        }

        Assert.AreSame(logical, server.LogicalConnection);
        Assert.AreEqual(1, server.Application.DispatcherConnectedCount);
        Assert.AreEqual(1, server.Application.LifetimeConnectedCount);
        Assert.AreEqual(0, server.Application.DispatcherDisconnectedCount);
        Assert.AreEqual(0, server.Application.LifetimeDisconnectedCount);

        logical.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(ObservationTimeout);
        await server.Application.LifetimeDisconnected.Task.WaitAsync(ObservationTimeout);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketProtocolSwitchWaitsForInFlightOldProtocolWrite()
    {
        await using var server = new RaidoTestServer();
        await server.StartAsync();
        await using var client = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync().WaitAsync(ObservationTimeout);
        var logical = server.LogicalConnection;
        await server.Application.DispatcherConnected.Task.WaitAsync(ObservationTimeout);

        server.Protocol.BlockWrites = true;
        Task? oldProtocolWrite = null;
        try
        {
            oldProtocolWrite = Task.Run(() => logical.WriteAsync(new RaidoTestMessage(0x41)).AsTask());
            await server.Protocol.WriteStarted.Task.WaitAsync(ObservationTimeout);

            var protocolB = new RaidoAlternateTestProtocol();
            var protocolSwitch = logical.SetProtocolAsync(protocolB, CancellationToken.None).AsTask();
            Assert.IsFalse(protocolSwitch.IsCompleted);

            server.Protocol.ReleaseWrite.TrySetResult();
            await oldProtocolWrite.WaitAsync(ObservationTimeout);
            using (var readTimeout = new CancellationTokenSource(ObservationTimeout))
            {
                CollectionAssert.AreEqual(
                    RaidoTestProtocol.Encode(0x41),
                    await client.ReadFrameAsync(readTimeout.Token));
            }

            await protocolSwitch.WaitAsync(ObservationTimeout);
            await logical.WriteAsync(new RaidoTestMessage(0x42));
            using (var readTimeout = new CancellationTokenSource(ObservationTimeout))
            {
                CollectionAssert.AreEqual(
                    RaidoAlternateTestProtocol.Encode(0x42),
                    await client.ReadFrameAsync(readTimeout.Token));
            }
        }
        finally
        {
            server.Protocol.ReleaseWrite.TrySetResult();
            if (oldProtocolWrite is not null)
            {
                await oldProtocolWrite.WaitAsync(ObservationTimeout);
            }
        }

        Assert.AreEqual(1, server.Application.DispatcherConnectedCount);
        Assert.AreEqual(1, server.Application.LifetimeConnectedCount);
        Assert.AreEqual(0, server.Application.DispatcherDisconnectedCount);
        Assert.AreEqual(0, server.Application.LifetimeDisconnectedCount);

        logical.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(ObservationTimeout);
        await server.Application.LifetimeDisconnected.Task.WaitAsync(ObservationTimeout);
    }
}

internal sealed class RaidoAlternateTestProtocol : IRaidoProtocol
{
    private const int FrameSize = 2;
    private const byte Marker = 0xB6;

    public string Name => "integration-b";
    public int Version => 1;

    public static byte[] Encode(byte id) => [Marker, id];

    public bool TryParseMessage(
        in ReadOnlySequence<byte> input,
        ref SequencePosition consumed,
        ref SequencePosition examined,
        [MaybeNullWhen(false)] out RaidoMessage message)
    {
        if (input.Length < FrameSize)
        {
            consumed = input.Start;
            examined = input.End;
            message = null;
            return false;
        }

        var frame = input.Slice(0, FrameSize).ToArray();
        if (frame[0] != Marker)
        {
            throw new InvalidDataException("The integration protocol B marker was not present.");
        }

        consumed = input.GetPosition(FrameSize);
        examined = consumed;
        message = new RaidoTestMessage(frame[1]);
        return true;
    }

    public void WriteMessage(RaidoMessage message, IBufferWriter<byte> output)
    {
        var id = message is RaidoTestMessage testMessage ? testMessage.Id : (byte)0xFF;
        var destination = output.GetSpan(FrameSize);
        destination[0] = Marker;
        destination[1] = id;
        output.Advance(FrameSize);
    }

    public ReadOnlyMemory<byte> GetMessageBytes(RaidoMessage message)
    {
        var id = message is RaidoTestMessage testMessage ? testMessage.Id : (byte)0xFF;
        return new byte[] { Marker, id };
    }

    public bool IsVersionSupported(int version) => version == Version;
}

internal sealed class TrackingProtocolLifetime : IAsyncDisposable
{
    private int _disposeCount;

    public int DisposeCount => Volatile.Read(ref _disposeCount);

    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _disposeCount);
        return ValueTask.CompletedTask;
    }
}
