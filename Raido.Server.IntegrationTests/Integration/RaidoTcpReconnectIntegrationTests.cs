using Raido.Server.IntegrationTests.Infrastructure;

namespace Raido.Server.IntegrationTests.Integration;

[TestClass]
[DoNotParallelize]
public sealed class RaidoTcpReconnectIntegrationTests
{
    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketReplacementPreservesOneLogicalConnection()
    {
        await using var server = new RaidoTestServer();
        await server.StartAsync();
        await using var clientA = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync();
        var logical = server.LogicalConnection;
        await server.Application.DispatcherConnected.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await clientA.SendAsync(RaidoTestProtocol.Encode(0x01));
        await server.Application.WaitForMessageAsync(0x01).WaitAsync(TimeSpan.FromSeconds(2));
        await clientA.DisposeAsync();
        await server.WaitForInitialPhysicalCloseAsync().WaitAsync(TimeSpan.FromSeconds(2));

        await using var clientB = await server.ConnectClientAsync();
        var replacement = await server.AcceptReplacementAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(server.ActivateReplacement(replacement));

        await clientB.SendAsync(RaidoTestProtocol.Encode(0x02));
        await server.Application.WaitForMessageAsync(0x02).WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreEqual(1, server.Application.DispatcherConnectedCount);
        Assert.AreEqual(1, server.Application.LifetimeConnectedCount);
        Assert.AreEqual(0, server.Application.DispatcherDisconnectedCount);
        Assert.AreEqual(0, server.Application.LifetimeDisconnectedCount);
        Assert.AreEqual(logical.ConnectionId, server.LogicalConnection.ConnectionId);
        Assert.AreSame(logical, server.LogicalConnection);

        logical.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await server.Application.LifetimeDisconnected.Task.WaitAsync(TimeSpan.FromSeconds(2));
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
        await server.WaitForLogicalConnectionAsync();

        await clientA.SendAsync(new byte[] { 0x11 });
        await server.WaitForPartialFrameAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsEmpty(server.Application.ReceivedMessageIds);

        await clientA.DisposeAsync();
        await server.WaitForInitialPhysicalCloseAsync().WaitAsync(TimeSpan.FromSeconds(2));
        await using var clientB = await server.ConnectClientAsync();
        var replacement = await server.AcceptReplacementAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(server.ActivateReplacement(replacement));

        await clientB.SendAsync(RaidoTestProtocol.Encode(0x22));
        await server.Application.WaitForMessageAsync(0x22).WaitAsync(TimeSpan.FromSeconds(2));
        await clientB.SendAsync(RaidoTestProtocol.Encode(0x23));
        await server.Application.WaitForMessageAsync(0x23).WaitAsync(TimeSpan.FromSeconds(2));

        CollectionAssert.AreEqual(new[] { 0x22, 0x23 }, server.Application.ReceivedMessageIds.ToArray());
        Assert.IsFalse(server.Application.ReceivedMessageIds.Contains(0x11));
        server.LogicalConnection.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketCloseWithoutReplacementStartsReconnectTimeout()
    {
        await using var server = new RaidoTestServer(TimeSpan.FromSeconds(2));
        await server.StartAsync();
        await using var client = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync();
        var logical = server.LogicalConnection;
        await server.Application.DispatcherConnected.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var stableClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stableClosedRegistration = logical.ConnectionAbortedToken.Register(
            static state => ((TaskCompletionSource)state!).TrySetResult(),
            stableClosed);
        await client.DisposeAsync();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(TimeSpan.FromSeconds(6));
        await server.Application.LifetimeDisconnected.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreEqual(1, server.Application.DispatcherDisconnectedCount);
        Assert.AreEqual(1, server.Application.LifetimeDisconnectedCount);
        Assert.IsTrue(stableClosed.Task.IsCompleted);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketReplacementBeforeTimeoutPreservesLogicalConnection()
    {
        await using var server = new RaidoTestServer(TimeSpan.FromSeconds(2));
        await server.StartAsync();
        await using var clientA = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync();
        var logical = server.LogicalConnection;
        await server.Application.DispatcherConnected.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await clientA.DisposeAsync();
        await server.WaitForInitialPhysicalCloseAsync().WaitAsync(TimeSpan.FromSeconds(2));

        await using var clientB = await server.ConnectClientAsync();
        var replacement = await server.AcceptReplacementAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(server.ActivateReplacement(replacement));
        await clientB.SendAsync(RaidoTestProtocol.Encode(0x31));
        await server.Application.WaitForMessageAsync(0x31).WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreEqual(0, server.Application.DispatcherDisconnectedCount);
        Assert.IsFalse(logical.ConnectionAbortedToken.IsCancellationRequested);

        logical.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual(1, server.Application.DispatcherDisconnectedCount);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketRapidReplacementObservesBClosedWhileAInputBoundaryIsPending()
    {
        await using var server = new RaidoTestServer();
        await server.StartAsync();
        await using var clientA = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync();
        server.Application.HoldDispatch = true;

        await clientA.SendAsync(RaidoTestProtocol.Encode(0x41));
        await server.Application.DispatchEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await clientA.DisposeAsync();
        await server.WaitForInitialPhysicalCloseAsync().WaitAsync(TimeSpan.FromSeconds(2));

        await using var clientB = await server.ConnectClientAsync();
        var replacementB = await server.AcceptReplacementAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(server.ActivateReplacement(replacementB));
        await clientB.DisposeAsync();
        await replacementB.Closed.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await using var clientC = await server.ConnectClientAsync();
        var replacementC = await server.AcceptReplacementAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(server.ActivateReplacement(replacementC));

        server.Application.ReleaseDispatch.TrySetResult();
        await clientC.SendAsync(RaidoTestProtocol.Encode(0x43));
        await server.Application.WaitForMessageAsync(0x43).WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreEqual(1, server.Application.DispatcherConnectedCount);
        Assert.AreEqual(0, server.Application.DispatcherDisconnectedCount);
        server.LogicalConnection.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task RealSocketDetachedOutputIsNotReplayedToReplacement()
    {
        await using var server = new RaidoTestServer();
        await server.StartAsync();
        await using var clientA = await server.ConnectClientAsync();
        await server.WaitForLogicalConnectionAsync();
        var logical = server.LogicalConnection;
        await server.Application.DispatcherConnected.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await logical.WriteAsync(new RaidoTestMessage(0x51));
        CollectionAssert.AreEqual(RaidoTestProtocol.Encode(0x51), await clientA.ReadFrameAsync());
        await clientA.DisposeAsync();
        await server.WaitForInitialPhysicalCloseAsync().WaitAsync(TimeSpan.FromSeconds(2));

        await logical.WriteAsync(new RaidoTestMessage(0x52));
        await using var clientB = await server.ConnectClientAsync();
        var replacement = await server.AcceptReplacementAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(server.ActivateReplacement(replacement));

        await logical.WriteAsync(new RaidoTestMessage(0x53));
        CollectionAssert.AreEqual(RaidoTestProtocol.Encode(0x53), await clientB.ReadFrameAsync());

        logical.Abort();
        await server.Application.DispatcherDisconnected.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }
}
