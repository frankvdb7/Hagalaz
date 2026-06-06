using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
using Hagalaz.Game.Messages.Protocol.Map;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests.Model.Maps.Regions
{
    [TestClass]
    public class MapRegionPartTests
    {
        private IMapper _mapper = null!;
        private IGroundItemBuilder _groundItemBuilder = null!;
        private MapRegionPart _mapRegionPart = null!;
        private ICharacter _character = null!;
        private IGameSession _session = null!;

        [TestInitialize]
        public void Setup()
        {
            _mapper = Substitute.For<IMapper>();
            _groundItemBuilder = Substitute.For<IGroundItemBuilder>();
            _mapRegionPart = new MapRegionPart(_mapper, _groundItemBuilder);
            _character = Substitute.For<ICharacter>();
            _session = Substitute.For<IGameSession>();
            _character.Session.Returns(_session);
            _character.Viewport.Returns(Substitute.For<IViewport>());
        }

        [TestMethod]
        public void SendUpdates_NoAcceptedUpdates_SendsNoMessages()
        {
            // Arrange
            var update = Substitute.For<IRegionPartUpdate>();
            update.CanUpdateFor(_character).Returns(false);
            var updates = new List<IRegionPartUpdate> { update };

            // Act
            _mapRegionPart.SendUpdates(_character, updates, false);

            // Assert
            _session.DidNotReceive().SendMessage(Arg.Any<RaidoMessage>());
        }

        [TestMethod]
        public void SendUpdates_AcceptedUpdates_SendsUpdateMessageFirst()
        {
            // Arrange
            var update = Substitute.For<IRegionPartUpdate>();
            update.CanUpdateFor(_character).Returns(true);
            var updates = new List<IRegionPartUpdate> { update };
            var message = Substitute.For<RaidoMessage>();
            _mapper.Map<RaidoMessage>(update).Returns(message);

            // Act
            _mapRegionPart.SendUpdates(_character, updates, false);

            // Assert
            Received.InOrder(() =>
            {
                _session.SendMessage(Arg.Any<MapRegionPartUpdateMessage>());
                _session.SendMessage(message);
            });
        }

        [TestMethod]
        public void SendUpdates_OnlyAcceptedUpdatesAreProcessed()
        {
            // Arrange
            var acceptedUpdate = Substitute.For<IRegionPartUpdate>();
            acceptedUpdate.CanUpdateFor(_character).Returns(true);
            var rejectedUpdate = Substitute.For<IRegionPartUpdate>();
            rejectedUpdate.CanUpdateFor(_character).Returns(false);

            var updates = new List<IRegionPartUpdate> { acceptedUpdate, rejectedUpdate };
            var message = Substitute.For<RaidoMessage>();
            _mapper.Map<RaidoMessage>(acceptedUpdate).Returns(message);

            // Act
            _mapRegionPart.SendUpdates(_character, updates, false);

            // Assert
            _session.Received(1).SendMessage(message);
            acceptedUpdate.Received(1).OnUpdatedFor(_character);
            rejectedUpdate.DidNotReceive().OnUpdatedFor(Arg.Any<ICharacter>());
            _mapper.DidNotReceive().Map<RaidoMessage>(rejectedUpdate);
        }

        [TestMethod]
        public void SendUpdates_LazyEnumerable_HandledCorrectly()
        {
            // Arrange
            var update = Substitute.For<IRegionPartUpdate>();
            update.CanUpdateFor(_character).Returns(true);

            IEnumerable<IRegionPartUpdate> GetUpdates()
            {
                yield return update;
            }

            var message = Substitute.For<RaidoMessage>();
            _mapper.Map<RaidoMessage>(update).Returns(message);

            // Act
            _mapRegionPart.SendUpdates(_character, GetUpdates(), false);

            // Assert
            _session.Received(1).SendMessage(Arg.Any<MapRegionPartUpdateMessage>());
            update.Received(1).OnUpdatedFor(_character);
        }

        [TestMethod]
        public void SendUpdates_ListFastPath_HandledCorrectly()
        {
            // Arrange
            var update = Substitute.For<IRegionPartUpdate>();
            update.CanUpdateFor(_character).Returns(true);
            var updates = new List<IRegionPartUpdate> { update };
            var message = Substitute.For<RaidoMessage>();
            _mapper.Map<RaidoMessage>(update).Returns(message);

            // Act
            _mapRegionPart.SendUpdates(_character, updates, false);

            // Assert
            _session.Received(1).SendMessage(Arg.Any<MapRegionPartUpdateMessage>());
            update.Received(1).OnUpdatedFor(_character);
        }

        [TestMethod]
        public void SendUpdates_ReadOnlyList_HandledCorrectly()
        {
            // Arrange
            var update = Substitute.For<IRegionPartUpdate>();
            update.CanUpdateFor(_character).Returns(true);
            IRegionPartUpdate[] updates = [ update ];
            var message = Substitute.For<RaidoMessage>();
            _mapper.Map<RaidoMessage>(update).Returns(message);

            // Act
            _mapRegionPart.SendUpdates(_character, updates, false);

            // Assert
            _session.Received(1).SendMessage(Arg.Any<MapRegionPartUpdateMessage>());
            update.Received(1).OnUpdatedFor(_character);
        }
    }
}
