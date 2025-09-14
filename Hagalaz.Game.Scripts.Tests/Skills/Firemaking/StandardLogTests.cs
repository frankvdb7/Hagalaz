using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Skills.Firemaking;
using Hagalaz.Game.Scripts.Skills.Fletching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;

namespace Hagalaz.Game.Scripts.Tests.Skills.Firemaking
{
    [TestClass]
    public class StandardLogTests
    {
        private IFiremakingService _firemakingService;
        private IFletchingSkillService _fletchingSkillService;
        private IRsTaskService _taskService;
        private IPathFinderProvider _pathFinderProvider;
        private IGroundItemBuilder _groundItemBuilder;
        private IGameObjectBuilder _gameObjectBuilder;
        private IMapRegionService _mapRegionService;
        private StandardLog _standardLog;

        [TestInitialize]
        public void TestInitialize()
        {
            _firemakingService = Substitute.For<IFiremakingService>();
            _fletchingSkillService = Substitute.For<IFletchingSkillService>();
            _taskService = Substitute.For<IRsTaskService>();
            _pathFinderProvider = Substitute.For<IPathFinderProvider>();
            _groundItemBuilder = Substitute.For<IGroundItemBuilder>();
            _gameObjectBuilder = Substitute.For<IGameObjectBuilder>();
            _mapRegionService = Substitute.For<IMapRegionService>();

            _standardLog = new StandardLog(
                _firemakingService,
                _fletchingSkillService,
                _taskService,
                _pathFinderProvider,
                _groundItemBuilder,
                _gameObjectBuilder,
                _mapRegionService
            );
        }

        [TestMethod]
        public async Task LightGroundLog_WhenObjectExists_ShouldNotCreateFire()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            character.Statistics.GetSkillLevel(Arg.Any<int>()).Returns(99);
            character.Inventory.GetById(FiremakingConstants.Tinderbox).Returns(Substitute.For<IItem>());

            var logItem = Substitute.For<IGroundItem>();
            var location = Substitute.For<ILocation>();
            logItem.Location.Returns(location);

            var item = Substitute.For<IItem>();
            item.Id.Returns(1);
            logItem.ItemOnGround.Returns(item);

            var region = Substitute.For<IMapRegion>();
            _mapRegionService.GetMapRegion(location.RegionId, location.Dimension, false, true).Returns(region);
            region.FindStandardGameObject(location.RegionLocalX, location.RegionLocalY, location.Z).Returns(Substitute.For<IGameObject>());

            var firemakingDefinition = new FiremakingDto
            {
                ItemId = 1,
                RequiredLevel = 1,
                FireObjectId = 2,
                Experience = 10,
                Ticks = 100
            };
            _firemakingService.FindByLogId(item.Id).Returns(Task.FromResult<FiremakingDto?>(firemakingDefinition));

            // Act
            await _standardLog.LightGroundLog(character, logItem);

            // Assert
            character.Received().SendChatMessage("You can't light a fire here.");
            region.DidNotReceive().Add(Arg.Any<IGameObject>());
        }
    }
}
