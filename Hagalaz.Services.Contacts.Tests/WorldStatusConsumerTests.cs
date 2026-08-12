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
using Microsoft.Extensions.Logging.Abstractions;
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
            var cleanupService = new WorldContactCleanupService(
                _characterServiceMock.Object,
                _contactSessions,
                NullLogger<WorldContactCleanupService>.Instance);
            _consumer = new WorldStatusConsumer(_worldSessions, cleanupService, _loggerMock.Object);
        }

        [TestMethod]
        public async Task Consume_WorldOfflineMessage_PublishesCorrectSignOutMessages()
        {
            const int worldId = 1;
            const uint masterId = 100;
            var contactDto = new CharacterDto
            {
                MasterId = masterId,
                DisplayName = "TestUser",
                PreviousDisplayName = "OldName"
            };

            _contactSessions.TryAdd(masterId, new ContactSessionContext(masterId, worldId, "World 1"));
            _worldSessions.TryAdd(worldId, new WorldSessionContext(worldId, "World 1", "instance-a", 1));
            _characterServiceMock.Setup(x => x.FindCharacterByIdAsync(masterId)).ReturnsAsync(contactDto);

            var contextMock = new Mock<ConsumeContext<WorldOfflineMessage>>();
            contextMock.Setup(x => x.Message).Returns(new WorldOfflineMessage(worldId, "instance-a", 1));

            await _consumer.Consume(contextMock.Object);

            _characterServiceMock.Verify(x => x.FindCharacterByIdAsync(masterId), Times.Once);
            contextMock.Verify(
                x => x.Publish(It.Is<ContactSignOutMessage>(message => message.Contact.MasterId == masterId), It.IsAny<CancellationToken>()),
                Times.Once);
            Assert.IsFalse(_worldSessions.TryGetValue(worldId, out _));
        }

        [TestMethod]
        public async Task Consume_OfflineSelectedGeneration_RetainsSurvivingGeneration()
        {
            const int worldId = 1;
            _worldSessions.ObserveOnline(new WorldSessionContext(worldId, "World 1", "instance-a", 1));
            _worldSessions.ObserveOnline(new WorldSessionContext(worldId, "World 1", "instance-b", 2));

            var contextMock = new Mock<ConsumeContext<WorldOfflineMessage>>();
            contextMock.Setup(x => x.Message).Returns(new WorldOfflineMessage(worldId, "instance-a", 1));

            await _consumer.Consume(contextMock.Object);

            Assert.IsTrue(_worldSessions.TryGetValue(worldId, out var replacement));
            Assert.AreEqual("instance-b", replacement!.InstanceId);
            contextMock.Verify(
                x => x.Publish(It.IsAny<ContactSignOutMessage>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Consume_StaleOfflineMessage_DoesNotRemoveReplacementWorld()
        {
            const int worldId = 1;
            _worldSessions.ObserveOnline(new WorldSessionContext(worldId, "World 1", "instance-b", 2));

            var contextMock = new Mock<ConsumeContext<WorldOfflineMessage>>();
            contextMock.Setup(x => x.Message).Returns(new WorldOfflineMessage(worldId, "instance-a", 1));

            await _consumer.Consume(contextMock.Object);

            Assert.IsTrue(_worldSessions.TryGetValue(worldId, out var replacement));
            Assert.AreEqual("instance-b", replacement!.InstanceId);
        }

        [TestMethod]
        public void WorldSessionStore_ExpiresCrashedGeneration()
        {
            const int worldId = 1;
            var now = DateTimeOffset.UtcNow;
            _worldSessions.ObserveOnline(new WorldSessionContext(
                worldId,
                "World 1",
                "instance-a",
                1,
                now.AddSeconds(1)),
                now);

            var updates = _worldSessions.Expire(now.AddSeconds(2));

            Assert.AreEqual(1, updates.Count);
            Assert.IsFalse(updates[0].IsAvailable);
            Assert.IsFalse(_worldSessions.TryGetValue(worldId, out _));
        }
    }
}
