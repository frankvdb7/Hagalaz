using Hagalaz.Game.Messages;
using Hagalaz.Services.Contacts.Consumers;
using Hagalaz.Services.Contacts.Services;
using MassTransit;
using Moq;

namespace Hagalaz.Services.Contacts.Tests;

[TestClass]
public sealed class SessionPresenceConsumerTests
{
    [TestMethod]
    public async Task WorldSignIn_ForwardsConnectionOwner()
    {
        var service = new Mock<IContactSessionService>();
        service
            .Setup(x => x.AddWorldSession(1, 42, 2, "world-b"))
            .Returns(Task.CompletedTask);
        var consumer = new WorldUserSignInOutConsumer(service.Object);
        var context = CreateContext(new WorldUserSignInMessage(42, 1, 2, "world-b"));

        await consumer.Consume(context.Object);

        service.Verify(x => x.AddWorldSession(1, 42, 2, "world-b"), Times.Once);
    }

    [TestMethod]
    public async Task WorldSignOut_ForwardsConnectionOwner()
    {
        var service = new Mock<IContactSessionService>();
        service
            .Setup(x => x.RemoveSession(42, 1, "world-a"))
            .Returns(Task.CompletedTask);
        var consumer = new WorldUserSignInOutConsumer(service.Object);
        var context = CreateContext(new WorldUserSignOutMessage(42, 1, 1, "world-a"));

        await consumer.Consume(context.Object);

        service.Verify(x => x.RemoveSession(42, 1, "world-a"), Times.Once);
    }

    [TestMethod]
    public async Task LobbySignIn_ForwardsConnectionOwner()
    {
        var service = new Mock<IContactSessionService>();
        service
            .Setup(x => x.AddLobbySession(1, 42, 1, "lobby-a"))
            .Returns(Task.CompletedTask);
        var consumer = new LobbyUserSignInOutConsumer(service.Object);
        var context = CreateContext(new LobbyUserSignInMessage(42, 1, 1, "lobby-a"));

        await consumer.Consume(context.Object);

        service.Verify(x => x.AddLobbySession(1, 42, 1, "lobby-a"), Times.Once);
    }

    [TestMethod]
    public async Task LobbySignOut_ForwardsConnectionOwner()
    {
        var service = new Mock<IContactSessionService>();
        service
            .Setup(x => x.RemoveSession(42, 1, "lobby-a"))
            .Returns(Task.CompletedTask);
        var consumer = new LobbyUserSignInOutConsumer(service.Object);
        var context = CreateContext(new LobbyUserSignOutMessage(42, 1, 1, "lobby-a"));

        await consumer.Consume(context.Object);

        service.Verify(x => x.RemoveSession(42, 1, "lobby-a"), Times.Once);
    }

    private static Mock<ConsumeContext<TMessage>> CreateContext<TMessage>(TMessage message)
        where TMessage : class
    {
        var context = new Mock<ConsumeContext<TMessage>>();
        context.SetupGet(x => x.Message).Returns(message);
        return context;
    }
}
