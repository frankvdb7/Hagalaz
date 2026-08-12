using Hagalaz.Game.Messages;
using Hagalaz.Services.Contacts.Consumers;
using Hagalaz.Services.Contacts.Services;
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
        private WorldSessionStore _worldSessions = null!;
        private Mock<IContactSessionService> _contactSessionServiceMock = null!;
        private Mock<ILogger<WorldStatusConsumer>> _loggerMock = null!;
        private WorldStatusConsumer _consumer = null!;

        [TestInitialize]
        public void Initialize()
        {
            _worldSessions = new WorldSessionStore();
            _contactSessionServiceMock = new Mock<IContactSessionService>();
            _loggerMock = new Mock<ILogger<WorldStatusConsumer>>();
            _consumer = new WorldStatusConsumer(_worldSessions, _contactSessionServiceMock.Object, _loggerMock.Object);
        }

        [TestMethod]
        public async Task Consume_WorldOfflineMessage_RemovesContactsThroughSessionService()
        {
            const int worldId = 1;
            _worldSessions.TryAdd(worldId, new WorldSessionContext(worldId, "World 1", "instance-a", 1));

            var contextMock = new Mock<ConsumeContext<WorldOfflineMessage>>();
            contextMock.Setup(x => x.Message).Returns(new WorldOfflineMessage(worldId, "instance-a", 1));

            await _consumer.Consume(contextMock.Object);

            _contactSessionServiceMock.Verify(x => x.RemoveWorldSessions(worldId), Times.Once);
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
            _contactSessionServiceMock.Verify(x => x.RemoveWorldSessions(worldId), Times.Never);
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
