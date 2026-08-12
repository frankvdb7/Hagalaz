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
        contactSessions.TryAdd(removedMasterId, new ContactSessionContext(removedMasterId, worldId, "World 1"));
        contactSessions.TryAdd(retainedMasterId, new ContactSessionContext(retainedMasterId, 2, "World 2"));

        var service = new ContactSessionService(
            characterService.Object,
            contactSessions,
            new WorldSessionStore(),
            publishEndpoint.Object,
            new Mock<IStringLocalizer<ContactSessionService>>().Object);

        await service.RemoveWorldSessions(worldId);

        Assert.IsFalse(contactSessions.TryGetValue(removedMasterId, out _));
        Assert.IsTrue(contactSessions.TryGetValue(retainedMasterId, out _));
        Assert.IsTrue(contactSessions.TryAdd(removedMasterId, new ContactSessionContext(removedMasterId, worldId, "World 1")));
        publishEndpoint.Verify(
            x => x.Publish(
                It.Is<ContactSignOutMessage>(message => message.Contact.MasterId == removedMasterId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
