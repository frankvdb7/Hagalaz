using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class LifecycleNotificationTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SendAsync_WhenConsumerNeverCompletes_CancelsTheSend(bool callerCancels)
    {
        var observedToken = CancellationToken.None;
        var mediator = Substitute.For<IGameMediator>();
        mediator.SendAsync(Arg.Any<LobbySignOutCommand>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                observedToken = call.Arg<CancellationToken>();
                return Task.Delay(Timeout.Infinite, observedToken);
            });
        using var caller = new CancellationTokenSource();
        if (callerCancels)
        {
            caller.CancelAfter(TimeSpan.FromMilliseconds(50));
        }

        var send = LifecycleNotification.SendAsync(
            mediator,
            new LobbySignOutCommand(42, 3, "lobby-a"),
            caller.Token,
            callerCancels ? TimeSpan.FromMinutes(1) : TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<TaskCanceledException>(() => send.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(observedToken.IsCancellationRequested);
    }
}
