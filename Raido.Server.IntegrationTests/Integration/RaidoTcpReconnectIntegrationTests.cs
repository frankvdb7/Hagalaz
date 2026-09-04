using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Raido.Common.Protocol;
using Raido.Server.IntegrationTests.Infrastructure;

namespace Raido.Server.IntegrationTests.Integration;

[TestClass]
[DoNotParallelize]
public sealed class RaidoTcpReconnectIntegrationTests
{
    private static readonly TimeSpan ObservationTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ReplacementReconnectTimeout = TimeSpan.FromSeconds(15);

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

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketReplacementPreservesOneLogicalConnection()
    {
        await using var server = new RaidoTestServer();
        await server.StartAsync();
        await using var clientA = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync().WaitAsync(ObservationTimeout);
        var logical = server.LogicalConnection;
        await server.Application.DispatcherConnected.Task.WaitAsync(ObservationTimeout);

        await clientA.SendAsync(RaidoTestProtocol.Encode(0x01));
        await server.Application.WaitForMessageAsync(0x01).WaitAsync(ObservationTimeout);
        await clientA.DisposeAsync();
        await server.WaitForInitialPhysicalCloseAsync().WaitAsync(ObservationTimeout);

        await using var clientB = await server.ConnectClientAsync();
        var replacement = await server.AcceptReplacementAsync().WaitAsync(ObservationTimeout);
        Assert.IsTrue(server.ActivateReplacement(replacement));

        await clientB.SendAsync(RaidoTestProtocol.Encode(0x02));
        await server.Application.WaitForMessageAsync(0x02).WaitAsync(ObservationTimeout);

        Assert.AreEqual(1, server.Application.DispatcherConnectedCount);
        Assert.AreEqual(1, server.Application.LifetimeConnectedCount);
        Assert.AreEqual(0, server.Application.DispatcherDisconnectedCount);
        Assert.AreEqual(0, server.Application.LifetimeDisconnectedCount);
        Assert.AreEqual(logical.ConnectionId, server.LogicalConnection.ConnectionId);
        Assert.AreSame(logical, server.LogicalConnection);

        logical.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(ObservationTimeout);
        await server.Application.LifetimeDisconnected.Task.WaitAsync(ObservationTimeout);
        Assert.AreEqual(1, server.Application.DispatcherDisconnectedCount);
        Assert.AreEqual(1, server.Application.LifetimeDisconnectedCount);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketPartialFrameDoesNotCrossReplacementBoundary()
    {
        await using var server = new RaidoTestServer();
        await server.StartAsync();
        await using var clientA = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync().WaitAsync(ObservationTimeout);

        await clientA.SendAsync(new byte[] { 0x11 });
        await server.WaitForPartialFrameAsync().WaitAsync(ObservationTimeout);
        Assert.IsEmpty(server.Application.ReceivedMessageIds);

        await clientA.DisposeAsync();
        await server.WaitForInitialPhysicalCloseAsync().WaitAsync(ObservationTimeout);
        await using var clientB = await server.ConnectClientAsync();
        var replacement = await server.AcceptReplacementAsync().WaitAsync(ObservationTimeout);
        Assert.IsTrue(server.ActivateReplacement(replacement));

        await clientB.SendAsync(RaidoTestProtocol.Encode(0x22));
        await server.Application.WaitForMessageAsync(0x22).WaitAsync(ObservationTimeout);
        await clientB.SendAsync(RaidoTestProtocol.Encode(0x23));
        await server.Application.WaitForMessageAsync(0x23).WaitAsync(ObservationTimeout);

        CollectionAssert.AreEqual(new[] { 0x22, 0x23 }, server.Application.ReceivedMessageIds.ToArray());
        Assert.IsFalse(server.Application.ReceivedMessageIds.Contains(0x11));
        server.LogicalConnection.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(ObservationTimeout);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketCloseWithoutReplacementStartsReconnectTimeout()
    {
        await using var server = new RaidoTestServer(TimeSpan.FromSeconds(2));
        await server.StartAsync();
        await using var client = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync().WaitAsync(ObservationTimeout);
        var logical = server.LogicalConnection;
        await server.Application.DispatcherConnected.Task.WaitAsync(ObservationTimeout);

        var stableClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stableClosedRegistration = logical.ConnectionAborted.Register(
            static state => ((TaskCompletionSource)state!).TrySetResult(),
            stableClosed);
        await client.DisposeAsync();
        await stableClosed.Task.WaitAsync(ObservationTimeout);
        await server.Application.DispatcherDisconnected.Task.WaitAsync(ObservationTimeout);
        await server.Application.LifetimeDisconnected.Task.WaitAsync(ObservationTimeout);

        Assert.AreEqual(1, server.Application.DispatcherDisconnectedCount);
        Assert.AreEqual(1, server.Application.LifetimeDisconnectedCount);
        Assert.IsTrue(stableClosed.Task.IsCompleted);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketReplacementBeforeTimeoutPreservesLogicalConnection()
    {
        await using var server = new RaidoTestServer(ReplacementReconnectTimeout);
        await server.StartAsync();
        await using var clientA = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync().WaitAsync(ObservationTimeout);
        var logical = server.LogicalConnection;
        await server.Application.DispatcherConnected.Task.WaitAsync(ObservationTimeout);
        await clientA.DisposeAsync();
        await server.WaitForInitialPhysicalCloseAsync().WaitAsync(ObservationTimeout);

        await using var clientB = await server.ConnectClientAsync();
        var replacement = await server.AcceptReplacementAsync().WaitAsync(ObservationTimeout);
        Assert.IsTrue(server.ActivateReplacement(replacement));
        await clientB.SendAsync(RaidoTestProtocol.Encode(0x31));
        await server.Application.WaitForMessageAsync(0x31).WaitAsync(ObservationTimeout);

        Assert.AreEqual(0, server.Application.DispatcherDisconnectedCount);
        Assert.IsFalse(logical.ConnectionAborted.IsCancellationRequested);

        logical.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(ObservationTimeout);
        Assert.AreEqual(1, server.Application.DispatcherDisconnectedCount);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketRapidReplacementObservesBClosedWhileAInputBoundaryIsPending()
    {
        await using var server = new RaidoTestServer();
        await server.StartAsync();
        await using var clientA = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync().WaitAsync(ObservationTimeout);
        server.Application.HoldDispatch = true;

        await clientA.SendAsync(RaidoTestProtocol.Encode(0x41));
        await server.Application.DispatchEntered.Task.WaitAsync(ObservationTimeout);
        await clientA.DisposeAsync();
        await server.WaitForInitialPhysicalCloseAsync().WaitAsync(ObservationTimeout);

        await using var clientB = await server.ConnectClientAsync();
        var replacementB = await server.AcceptReplacementAsync().WaitAsync(ObservationTimeout);
        Assert.IsTrue(server.ActivateReplacement(replacementB));
        await clientB.DisposeAsync();
        await replacementB.Closed.Task.WaitAsync(ObservationTimeout);

        await using var clientC = await server.ConnectClientAsync();
        var replacementC = await server.AcceptReplacementAsync().WaitAsync(ObservationTimeout);
        Assert.IsTrue(server.ActivateReplacement(replacementC));

        server.Application.ReleaseDispatch.TrySetResult();
        await clientC.SendAsync(RaidoTestProtocol.Encode(0x43));
        await server.Application.WaitForMessageAsync(0x43).WaitAsync(ObservationTimeout);

        Assert.AreEqual(1, server.Application.DispatcherConnectedCount);
        Assert.AreEqual(0, server.Application.DispatcherDisconnectedCount);
        server.LogicalConnection.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(ObservationTimeout);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketDetachedOutputIsNotReplayedToReplacement()
    {
        await using var server = new RaidoTestServer();
        await server.StartAsync();
        await using var clientA = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync().WaitAsync(ObservationTimeout);
        var logical = server.LogicalConnection;
        await server.Application.DispatcherConnected.Task.WaitAsync(ObservationTimeout);

        await logical.WriteAsync(new RaidoTestMessage(0x51));
        using (var readTimeout = new CancellationTokenSource(ObservationTimeout))
        {
            CollectionAssert.AreEqual(
                RaidoTestProtocol.Encode(0x51),
                await clientA.ReadFrameAsync(readTimeout.Token));
        }
        await clientA.DisposeAsync();
        await server.WaitForInitialPhysicalCloseAsync().WaitAsync(ObservationTimeout);

        await logical.WriteAsync(new RaidoTestMessage(0x52));
        await using var clientB = await server.ConnectClientAsync();
        var replacement = await server.AcceptReplacementAsync().WaitAsync(ObservationTimeout);
        Assert.IsTrue(server.ActivateReplacement(replacement));

        await logical.WriteAsync(new RaidoTestMessage(0x53));
        using (var readTimeout = new CancellationTokenSource(ObservationTimeout))
        {
            CollectionAssert.AreEqual(
                RaidoTestProtocol.Encode(0x53),
                await clientB.ReadFrameAsync(readTimeout.Token));
        }

        logical.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(ObservationTimeout);
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
