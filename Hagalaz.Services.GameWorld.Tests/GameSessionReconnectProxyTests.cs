using System.Collections.Concurrent;
using Hagalaz.Services.GameWorld.Network.Model;
using Raido.Common.Protocol;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class GameSessionReconnectProxyTests
{
    [TestMethod]
    public async Task OrdinarySendWaitsForReconnectProxyScopeAndUsesTheNormalProxy()
    {
        var normalProxy = new RecordingProxy();
        var reconnectProxy = new RecordingProxy();
        var session = new GameSession(17, "connection", normalProxy);
        var scopeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseScope = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var reconnectTask = Task.Run(() => session.ExecuteWithClientProxy(reconnectProxy, () =>
        {
            scopeEntered.SetResult();
            releaseScope.Task.GetAwaiter().GetResult();
            session.SendMessage(new TestMessage());
        }));

        await scopeEntered.Task;
        var ordinaryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var ordinaryTask = Task.Run(() =>
        {
            ordinaryStarted.SetResult();
            session.SendMessage(new TestMessage());
        });
        await ordinaryStarted.Task;

        Assert.IsFalse(ordinaryTask.IsCompleted);
        Assert.AreEqual(0, normalProxy.Messages.Count);

        releaseScope.SetResult();
        await reconnectTask;
        await ordinaryTask;

        Assert.AreEqual(1, reconnectProxy.Messages.Count);
        Assert.AreEqual(1, normalProxy.Messages.Count);
    }

    private sealed class TestMessage : RaidoMessage;

    private sealed class RecordingProxy : IRaidoClientProxy
    {
        public ConcurrentQueue<RaidoMessage> Messages { get; } = new();

        public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
            where TMessage : RaidoMessage
        {
            Messages.Enqueue(message);
            return Task.CompletedTask;
        }
    }
}
