using Hagalaz.Contacts.Messages;
using Hagalaz.Services.Contacts.Services;
using Hagalaz.Services.Contacts.Services.Model;
using Hagalaz.Services.Contacts.Store;
using Hagalaz.Services.Contacts.Store.Model;
using MassTransit;
using Microsoft.Extensions.Localization;
using Moq;

namespace Hagalaz.Services.Contacts.Tests;

[TestClass]
public sealed class ContactSessionServiceTests
{
    [TestMethod]
    public async Task RemoveWorldSessions_RemovesOnlyMatchingWorldAndPublishesSignOut()
    {
        const int worldId = 1;
        const uint removedMasterId = 100;
        const uint retainedMasterId = 200;

        var characterService = new Mock<ICharacterService>();
        characterService.Setup(x => x.FindCharacterByIdAsync(removedMasterId)).ReturnsAsync(new CharacterDto
        {
            MasterId = removedMasterId,
            DisplayName = "RemovedUser"
        });

        var publishEndpoint = new Mock<IPublishEndpoint>();
        publishEndpoint
            .Setup(x => x.Publish(It.IsAny<ContactSignOutMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var contactSessions = new ContactSessionStore();
        contactSessions.TrySetNewerSession(new ContactSessionContext(removedMasterId, worldId, "World 1", 1, "removed"));
        contactSessions.TrySetNewerSession(new ContactSessionContext(retainedMasterId, 2, "World 2", 1, "retained"));

        var service = new ContactSessionService(
            characterService.Object,
            contactSessions,
            new WorldSessionStore(),
            publishEndpoint.Object,
            new Mock<IStringLocalizer<ContactSessionService>>().Object);

        await service.RemoveWorldSessions(worldId);

        Assert.IsFalse(contactSessions.TryGetValue(removedMasterId, out _));
        Assert.IsTrue(contactSessions.TryGetValue(retainedMasterId, out _));
        Assert.IsTrue(contactSessions.TrySetNewerSession(new ContactSessionContext(removedMasterId, worldId, "World 1", 1, "removed")));
        publishEndpoint.Verify(
            x => x.Publish(
                It.Is<ContactSignOutMessage>(message =>
                    message.Contact.MasterId == removedMasterId &&
                    message.SessionGeneration == 1 &&
                    message.ConnectionId == "removed"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task RemoveWorldSessions_DoesNotRemoveSameWorldReplacementSession()
    {
        const int worldId = 1;
        const uint firstMasterId = 100;
        const uint secondMasterId = 200;
        var replacementMasterId = 0u;

        var firstSession = new ContactSessionContext(firstMasterId, worldId, "World 1", 1, "first");
        var replacedSession = new ContactSessionContext(secondMasterId, worldId, "World 1", 1, "replaced");
        var contactSessions = new ContactSessionStore();
        contactSessions.TrySetNewerSession(firstSession);
        contactSessions.TrySetNewerSession(replacedSession);

        var characterService = new Mock<ICharacterService>();
        var replacementAdded = false;
        characterService
            .Setup(x => x.FindCharacterByIdAsync(It.IsAny<uint>()))
            .Returns((uint masterId) =>
            {
                if (!replacementAdded)
                {
                    replacementAdded = true;
                    replacementMasterId = masterId;
                    Assert.IsTrue(contactSessions.TrySetNewerSession(
                        new ContactSessionContext(masterId, worldId, "World 1", 2, "replacement")));
                }

                return ValueTask.FromResult<CharacterDto?>(new CharacterDto
                {
                    MasterId = masterId,
                    DisplayName = masterId == firstMasterId ? "FirstUser" : "OtherUser"
                });
            });

        var publishEndpoint = new Mock<IPublishEndpoint>();
        publishEndpoint
            .Setup(x => x.Publish(It.IsAny<ContactSignOutMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ContactSessionService(
            characterService.Object,
            contactSessions,
            new WorldSessionStore(),
            publishEndpoint.Object,
            new Mock<IStringLocalizer<ContactSessionService>>().Object);

        await service.RemoveWorldSessions(worldId);

        Assert.AreNotEqual(0u, replacementMasterId);
        Assert.IsTrue(contactSessions.TryGetValue(replacementMasterId, out var replacement));
        Assert.AreEqual(worldId, replacement!.WorldId);
        var originalReplacementSession = replacementMasterId == firstMasterId ? firstSession : replacedSession;
        Assert.AreNotEqual(originalReplacementSession.SessionGeneration, replacement.SessionGeneration);
        publishEndpoint.Verify(
            x => x.Publish(
                It.IsAny<ContactSignOutMessage>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [TestMethod]
    public async Task WorldSignInBeforeStaleLobbySignOut_KeepsWorldPresenceOwnedByNewConnection()
    {
        var contactSessions = new ContactSessionStore();
        var worldSessions = new WorldSessionStore();
        worldSessions.TryAdd(1, new WorldSessionContext(1, "World 1"));
        var characterService = CreateCharacterService();
        var publishEndpoint = CreatePublishEndpoint();
        var service = CreateService(contactSessions, worldSessions, characterService, publishEndpoint);

        await service.AddLobbySession(1, 42, 1, "lobby-a");
        await service.AddWorldSession(1, 42, 2, "world-b");
        await service.RemoveSession(42, 1, "lobby-a");

        Assert.IsTrue(contactSessions.TryGetValue(42, out var session));
        Assert.AreEqual("world-b", session!.ConnectionId);
        Assert.AreEqual(1, session.WorldId);
        publishEndpoint.Verify(
            x => x.Publish(It.IsAny<ContactSignOutMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task StaleLobbySignOutBeforeWorldSignIn_StillEstablishesWorldPresence()
    {
        var contactSessions = new ContactSessionStore();
        var worldSessions = new WorldSessionStore();
        worldSessions.TryAdd(1, new WorldSessionContext(1, "World 1"));
        var service = CreateService(
            contactSessions,
            worldSessions,
            CreateCharacterService(),
            CreatePublishEndpoint());

        await service.AddLobbySession(1, 42, 1, "lobby-a");
        await service.RemoveSession(42, 1, "lobby-a");
        await service.AddWorldSession(1, 42, 2, "world-b");

        Assert.IsTrue(contactSessions.TryGetValue(42, out var session));
        Assert.AreEqual("world-b", session!.ConnectionId);
        Assert.AreEqual(1, session.WorldId);
    }

    [TestMethod]
    public async Task StaleWorldSignOut_DoesNotRemoveNewerWorldPresence()
    {
        var contactSessions = new ContactSessionStore();
        var worldSessions = new WorldSessionStore();
        worldSessions.TryAdd(1, new WorldSessionContext(1, "World 1"));
        var publishEndpoint = CreatePublishEndpoint();
        var service = CreateService(contactSessions, worldSessions, CreateCharacterService(), publishEndpoint);

        await service.AddWorldSession(1, 42, 1, "world-a");
        await service.AddWorldSession(1, 42, 2, "world-b");
        await service.RemoveSession(42, 1, "world-a");

        Assert.IsTrue(contactSessions.TryGetValue(42, out var session));
        Assert.AreEqual("world-b", session!.ConnectionId);
        publishEndpoint.Verify(
            x => x.Publish(It.IsAny<ContactSignOutMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CurrentWorldSignOut_RemovesOnlyTheOwnedPresence()
    {
        var contactSessions = new ContactSessionStore();
        var worldSessions = new WorldSessionStore();
        worldSessions.TryAdd(1, new WorldSessionContext(1, "World 1"));
        var publishEndpoint = CreatePublishEndpoint();
        var service = CreateService(contactSessions, worldSessions, CreateCharacterService(), publishEndpoint);

        await service.AddWorldSession(1, 42, 2, "world-b");
        await service.RemoveSession(42, 2, "world-b");

        Assert.IsFalse(contactSessions.TryGetValue(42, out _));
        publishEndpoint.Verify(
            x => x.Publish(
                It.Is<ContactSignOutMessage>(message =>
                    message.Contact.MasterId == 42 &&
                    message.SessionGeneration == 2 &&
                    message.ConnectionId == "world-b"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task DuplicateWorldSignInForCurrentConnection_DoesNotPublishDuplicatePresence()
    {
        var contactSessions = new ContactSessionStore();
        var worldSessions = new WorldSessionStore();
        worldSessions.TryAdd(1, new WorldSessionContext(1, "World 1"));
        var publishEndpoint = CreatePublishEndpoint();
        var service = CreateService(contactSessions, worldSessions, CreateCharacterService(), publishEndpoint);

        await service.AddWorldSession(1, 42, 2, "world-b");
        await service.AddWorldSession(1, 42, 2, "world-b");

        publishEndpoint.Verify(
            x => x.Publish(
                It.Is<ContactSignInMessage>(message => message.Contact.MasterId == 42 && message.Contact.WorldId == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
        publishEndpoint.Verify(
            x => x.Publish(It.IsAny<ContactSignOutMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task SameConnectionLobbyToWorldPromotion_ReplacesLobbyPresence()
    {
        var contactSessions = new ContactSessionStore();
        var worldSessions = new WorldSessionStore();
        worldSessions.TryAdd(1, new WorldSessionContext(1, "World 1"));
        var publishEndpoint = CreatePublishEndpoint();
        var service = CreateService(contactSessions, worldSessions, CreateCharacterService(), publishEndpoint);

        await service.AddLobbySession(1, 42, 1, "shared-connection");
        await service.AddWorldSession(1, 42, 2, "shared-connection");

        Assert.IsTrue(contactSessions.TryGetValue(42, out var session));
        Assert.AreEqual(2, session!.SessionGeneration);
        Assert.AreEqual(1, session.WorldId);
        publishEndpoint.Verify(
            x => x.Publish(It.IsAny<ContactSignInMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        publishEndpoint.Verify(
            x => x.Publish(It.IsAny<ContactSignOutMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task DelayedWorldSignOutAfterLobbyReplacement_DoesNotRemoveLobbyPresence()
    {
        var contactSessions = new ContactSessionStore();
        var worldSessions = new WorldSessionStore();
        worldSessions.TryAdd(1, new WorldSessionContext(1, "World 1"));
        var service = CreateService(contactSessions, worldSessions, CreateCharacterService(), CreatePublishEndpoint());

        await service.AddWorldSession(1, 42, 1, "world-a");
        await service.AddLobbySession(1, 42, 2, "lobby-b");
        await service.RemoveSession(42, 1, "world-a");

        Assert.IsTrue(contactSessions.TryGetValue(42, out var session));
        Assert.AreEqual(2, session!.SessionGeneration);
        Assert.AreEqual("lobby-b", session.ConnectionId);
    }

    [TestMethod]
    public async Task StaleWorldSignInAfterNewerWorldPresence_DoesNotReplaceCurrentPresence()
    {
        var contactSessions = new ContactSessionStore();
        var worldSessions = new WorldSessionStore();
        worldSessions.TryAdd(1, new WorldSessionContext(1, "World 1"));
        var publishEndpoint = CreatePublishEndpoint();
        var service = CreateService(contactSessions, worldSessions, CreateCharacterService(), publishEndpoint);

        await service.AddWorldSession(1, 42, 2, "world-b");
        await service.AddWorldSession(1, 42, 1, "world-a");

        Assert.IsTrue(contactSessions.TryGetValue(42, out var session));
        Assert.AreEqual(2, session!.SessionGeneration);
        Assert.AreEqual("world-b", session.ConnectionId);
        publishEndpoint.Verify(
            x => x.Publish(It.IsAny<ContactSignInMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
        publishEndpoint.Verify(
            x => x.Publish(It.IsAny<ContactSignOutMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task StaleLobbySignInAfterNewerWorldPresence_DoesNotDowngradeWorldPresence()
    {
        var contactSessions = new ContactSessionStore();
        var worldSessions = new WorldSessionStore();
        worldSessions.TryAdd(1, new WorldSessionContext(1, "World 1"));
        var publishEndpoint = CreatePublishEndpoint();
        var service = CreateService(contactSessions, worldSessions, CreateCharacterService(), publishEndpoint);

        await service.AddWorldSession(1, 42, 2, "world-b");
        await service.AddLobbySession(1, 42, 1, "lobby-a");

        Assert.IsTrue(contactSessions.TryGetValue(42, out var session));
        Assert.AreEqual(2, session!.SessionGeneration);
        Assert.AreEqual("world-b", session.ConnectionId);
        publishEndpoint.Verify(
            x => x.Publish(It.IsAny<ContactSignInMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
        publishEndpoint.Verify(
            x => x.Publish(It.IsAny<ContactSignOutMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ContactSessionService CreateService(
        ContactSessionStore contactSessions,
        WorldSessionStore worldSessions,
        Mock<ICharacterService> characterService,
        Mock<IPublishEndpoint> publishEndpoint)
    {
        var localizer = new Mock<IStringLocalizer<ContactSessionService>>();
        localizer.Setup(x => x["Lobby"]).Returns(new LocalizedString("Lobby", "Lobby"));
        return new ContactSessionService(
            characterService.Object,
            contactSessions,
            worldSessions,
            publishEndpoint.Object,
            localizer.Object);
    }

    private static Mock<ICharacterService> CreateCharacterService()
    {
        var characterService = new Mock<ICharacterService>();
        characterService
            .Setup(x => x.FindCharacterByIdAsync(It.IsAny<uint>()))
            .Returns((uint masterId) => ValueTask.FromResult<CharacterDto?>(new CharacterDto
            {
                MasterId = masterId,
                DisplayName = $"User {masterId}"
            }));
        return characterService;
    }

    private static Mock<IPublishEndpoint> CreatePublishEndpoint()
    {
        var publishEndpoint = new Mock<IPublishEndpoint>();
        publishEndpoint
            .Setup(x => x.Publish(It.IsAny<ContactSignInMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        publishEndpoint
            .Setup(x => x.Publish(It.IsAny<ContactSignOutMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return publishEndpoint;
    }
}
