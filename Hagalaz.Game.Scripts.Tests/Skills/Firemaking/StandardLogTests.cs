using System;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using System.Threading;
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
        public void LightInventoryLog_WhenDropFails_ShouldReturnFalseAndSendMessage()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var logItem = Substitute.For<IItem>();
            var itemScript = Substitute.For<IItemScript>();

            logItem.ItemScript.Returns(itemScript);
            character.Inventory.GetInstanceSlot(logItem).Returns(0);
            itemScript.DropItem(logItem, character).Returns(false);

            var firemakingDefinition = new FiremakingDto
            {
                ItemId = 1,
                RequiredLevel = 1,
                FireObjectId = 2,
                Experience = 10,
                Ticks = 100
            };
            _firemakingService.FindByLogId(logItem.Id).Returns(Task.FromResult<FiremakingDto?>(firemakingDefinition));

            // Act
            var result = _standardLog.LightInventoryLog(character, logItem);

            // Assert
            Assert.IsFalse(result);
            character.Received().SendChatMessage("You can't drop the logs here to make a fire.");
        }

        [TestMethod]
        public void LightGroundLog_ShouldFaceLogLocation()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var logItem = Substitute.For<IGroundItem>();
            var location = Location.Create(1, 1, 0, 0);
            var characterLocation = Location.Create(1, 2, 0, 0);
            var item = Substitute.For<IItem>();
            item.Id.Returns(1511);
            var region = Substitute.For<IMapRegion>();
            character.Location.Returns(characterLocation);
            character.Statistics.GetSkillLevel(Arg.Any<int>()).Returns(99);
            var firemakingDefinition = new FiremakingDto
            {
                ItemId = 1511,
                RequiredLevel = 1,
                FireObjectId = 2,
                Experience = 10,
                Ticks = 100
            };

            logItem.Location.Returns(location);
            logItem.ItemOnGround.Returns(item);
            _firemakingService.FindByLogId(1511).Returns(Task.FromResult<FiremakingDto?>(firemakingDefinition));
            _mapRegionService.GetMapRegion(location.RegionId, location.Dimension, false, true).Returns(region);
            region.FindStandardGameObject(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns((IGameObject)null);
            character.Inventory.GetById(FiremakingConstants.Tinderbox).Returns(Substitute.For<IItem>());

            ITaskItem capturedTask = null;
            character.When(x => x.QueueTask(Arg.Any<ITaskItem>()))
                .Do(callInfo =>
                {
                    capturedTask = callInfo.Arg<ITaskItem>();
                });

            _taskService.When(x => x.Schedule(Arg.Any<FiremakingTask>()))
                .Do(callInfo =>
                {
                    var firemakingTask = callInfo.Arg<FiremakingTask>();
                    firemakingTask.Tick();
                });

            // Act
            _standardLog.ItemClickedOnGroundPerform(logItem, GroundItemClickType.Option4Click, character);

            if (capturedTask is RsAsyncTask rsAsyncTask)
            {
                rsAsyncTask.Tick();
            }
            else if (capturedTask is RsTask rsTask)
            {
                rsTask.Tick();
            }

            // Assert
            character.Received().FaceLocation(location);
        }
    }
}
