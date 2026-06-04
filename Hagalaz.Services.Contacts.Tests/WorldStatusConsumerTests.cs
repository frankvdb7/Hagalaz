using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using Hagalaz.Game.Messages;
using Hagalaz.Services.Contacts.Consumers;
using Hagalaz.Services.Contacts.Services;
using Hagalaz.Services.Contacts.Services.Model;
using Hagalaz.Services.Contacts.Store;
using Hagalaz.Services.Contacts.Store.Model;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace Hagalaz.Services.Contacts.Tests
{
    [TestClass]
    public class WorldStatusConsumerTests
    {
        private Mock<ICharacterService> _characterServiceMock = null!;
        private WorldSessionStore _worldSessions = null!;
        private ContactSessionStore _contactSessions = null!;
        private Mock<ILogger<WorldStatusConsumer>> _loggerMock = null!;
        private WorldStatusConsumer _consumer = null!;

        [TestInitialize]
        public void Initialize()
        {
            _characterServiceMock = new Mock<ICharacterService>();
            _worldSessions = new WorldSessionStore();
            _contactSessions = new ContactSessionStore();
            _loggerMock = new Mock<ILogger<WorldStatusConsumer>>();
            _consumer = new WorldStatusConsumer(_characterServiceMock.Object, _worldSessions, _contactSessions, _loggerMock.Object);
        }

        [TestMethod]
        public async Task Consume_WorldOfflineMessage_PublishesCorrectSignOutMessages()
        {
            // Arrange
            var worldId = 1;
            var masterId = 100u;
            var contactDto = new CharacterDto
            {
                MasterId = masterId,
                DisplayName = "TestUser",
                PreviousDisplayName = "OldName"
            };

            _contactSessions.TryAdd(masterId, new ContactSessionContext(masterId, worldId, "World 1"));
            _worldSessions.TryAdd(worldId, new WorldSessionContext(worldId, "World 1"));

            _characterServiceMock.Setup(x => x.FindCharacterByIdAsync(masterId))
                .ReturnsAsync(contactDto);

            var contextMock = new Mock<ConsumeContext<WorldOfflineMessage>>();
            contextMock.Setup(x => x.Message).Returns(new WorldOfflineMessage(worldId));

            // Act
            await _consumer.Consume(contextMock.Object);

            // Assert
            _characterServiceMock.Verify(x => x.FindCharacterByIdAsync(masterId), Times.AtLeastOnce);
            // Verify that Publish was called with a ContactSignOutMessage
            contextMock.Verify(x => x.Publish(It.Is<ContactSignOutMessage>(m => m.Contact.MasterId == masterId), It.IsAny<CancellationToken>()), Times.Once);
            Assert.IsFalse(_worldSessions.ContainsKey(worldId));
        }
    }
}
