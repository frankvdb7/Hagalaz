using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Mediator.Consumers;
using MassTransit;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class SignOutCommandConsumerTests
{
    [TestMethod]
    public async Task Consume_PropagatesCancellationToTheBrokerPublish()
    {
        using var cancellation = new CancellationTokenSource();
        var bus = Substitute.For<IBus>();
        var options = Options.Create(new WorldOptions());
        var worldContext = Substitute.For<ConsumeContext<WorldSignOutCommand>>();
        worldContext.Message.Returns(new WorldSignOutCommand(42, 3, "a"));
        worldContext.CancellationToken.Returns(cancellation.Token);
        var lobbyContext = Substitute.For<ConsumeContext<LobbySignOutCommand>>();
        lobbyContext.Message.Returns(new LobbySignOutCommand(42, 3, "a"));
        lobbyContext.CancellationToken.Returns(cancellation.Token);

        await new WorldSignOutCommandConsumer(bus, options).Consume(worldContext);
        await new LobbySignOutCommandConsumer(bus, options).Consume(lobbyContext);

        await bus.Received(1).Publish(Arg.Any<WorldUserSignOutMessage>(), cancellation.Token);
        await bus.Received(1).Publish(Arg.Any<LobbyUserSignOutMessage>(), cancellation.Token);
    }
}
