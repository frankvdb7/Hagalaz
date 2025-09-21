using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Skills.Firemaking;
using Hagalaz.Game.Scripts.Skills.Fletching;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Maps;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.GameObjects;

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
        public void Setup()
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
                _mapRegionService);
        }

        [TestMethod]
        public void UseItemOnItem_WithTinderbox_ShouldLightLog()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var tinderboxItem = Substitute.For<IItem>();
            var logItem = Substitute.For<IItem>();
            var itemScript = Substitute.For<IItemScript>();
            var groundItemService = Substitute.For<IGroundItemService>();
            var groundItem = Substitute.For<IGroundItem>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            var firemakingDefinition = new FiremakingDto { ItemId = 1, RequiredLevel = 1, FireObjectId = 1, Experience = 1, Ticks = 1 };

            tinderboxItem.Id.Returns(FiremakingConstants.Tinderbox);
            logItem.ItemScript.Returns(itemScript);
            character.Inventory.GetInstanceSlot(logItem).Returns(0);
            _firemakingService.FindByLogId(logItem.Id).Returns(Task.FromResult<FiremakingDto?>(firemakingDefinition));
            itemScript.DropItem(logItem, character).Returns(true);
            character.ServiceProvider.Returns(serviceProvider);
            serviceProvider.GetService(typeof(IGroundItemService)).Returns(groundItemService);
            groundItemService.FindAllGroundItems(Arg.Any<ILocation>()).Returns(new List<IGroundItem> { groundItem });
            groundItem.ItemOnGround.Returns(logItem);

            // Act
            var result = _standardLog.UseItemOnItem(tinderboxItem, logItem, character);

            // Assert
            Assert.IsTrue(result);
            character.Received().QueueTask(Arg.Any<ITaskItem>());
        }

        [TestMethod]
        public async Task LightGroundLog_InsufficientLevel_ShouldSendErrorMessage()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var groundItem = Substitute.For<IGroundItem>();
            var logItem = Substitute.For<IItem>();
            var statistics = Substitute.For<ICharacterStatistics>();
            var firemakingDefinition = new FiremakingDto { ItemId = 1, RequiredLevel = 99, FireObjectId = 1, Experience = 1, Ticks = 1 };

            groundItem.ItemOnGround.Returns(logItem);
            _firemakingService.FindByLogId(logItem.Id).Returns(Task.FromResult<FiremakingDto?>(firemakingDefinition));
            character.Statistics.Returns(statistics);
            statistics.GetSkillLevel(StatisticsConstants.Firemaking).Returns(1);

            // Act
            await _standardLog.LightGroundLog(character, groundItem);

            // Assert
            character.Received().SendChatMessage("You need firemaking level of 99 to light this log.");
        }

        [TestMethod]
        public async Task LightGroundLog_InvalidLocation_ShouldSendErrorMessage()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var groundItem = Substitute.For<IGroundItem>();
            var logItem = Substitute.For<IItem>();
            var statistics = Substitute.For<ICharacterStatistics>();
            var mapRegion = Substitute.For<IMapRegion>();
            var location = Substitute.For<ILocation>();
            var firemakingDefinition = new FiremakingDto { ItemId = 1, RequiredLevel = 1, FireObjectId = 1, Experience = 1, Ticks = 1 };

            groundItem.ItemOnGround.Returns(logItem);
            groundItem.Location.Returns(location);
            _firemakingService.FindByLogId(logItem.Id).Returns(Task.FromResult<FiremakingDto?>(firemakingDefinition));
            character.Statistics.Returns(statistics);
            statistics.GetSkillLevel(StatisticsConstants.Firemaking).Returns(99);
            _mapRegionService.GetMapRegion(Arg.Any<int>(), Arg.Any<int>(), false, true).Returns(mapRegion);
            mapRegion.FindStandardGameObject(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns(Substitute.For<IGameObject>());

            // Act
            await _standardLog.LightGroundLog(character, groundItem);

            // Assert
            character.Received().SendChatMessage("You can't light a fire here.");
        }

        [TestMethod]
        public void UseItemOnItem_WithKnife_ShouldTryFletch()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var knifeItem = Substitute.For<IItem>();
            var logItem = Substitute.For<IItem>();

            // Act
            _standardLog.UseItemOnItem(knifeItem, logItem, character);

            // Assert
            _fletchingSkillService.Received().TryFletchWood(character, knifeItem, logItem);
        }

        [TestMethod]
        public void UseItemOnItem_WithInvalidItem_ShouldReturnFalse()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var invalidItem = Substitute.For<IItem>();
            var logItem = Substitute.For<IItem>();

            _fletchingSkillService.TryFletchWood(character, invalidItem, logItem).Returns(false);

            // Act
            var result = _standardLog.UseItemOnItem(invalidItem, logItem, character);

            // Assert
            Assert.IsFalse(result);
            character.DidNotReceive().QueueTask(Arg.Any<ITaskItem>());
        }
    }
}
