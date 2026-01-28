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
using Hagalaz.Game.Abstractions.Collections;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Game.Scripts.Tests.Skills.Firemaking
{
    [TestClass]
    public class StandardLogTests
    {
        private IFiremakingService _firemakingService = null!;
        private IFletchingSkillService _fletchingSkillService = null!;
        private IRsTaskService _taskService = null!;
        private IPathFinderProvider _pathFinderProvider = null!;
        private IGroundItemBuilder _groundItemBuilder = null!;
        private IGameObjectBuilder _gameObjectBuilder = null!;
        private IMapRegionService _mapRegionService = null!;
        private StandardLog _standardLog = null!;

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

        [DataTestMethod]
        [DataRow(true, "Tinderbox on Log")]
        [DataRow(false, "Log on Tinderbox")]
        public void UseItemOnItem_Success_ShouldReturnTrueAndQueueTask(bool tinderboxFirst, string displayName)
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var tinderboxItem = Substitute.For<IItem>();
            var logItem = Substitute.For<IItem>();
            var itemScript = Substitute.For<IItemScript>();
            var groundItemService = Substitute.For<IGroundItemService>();
            var groundItem = Substitute.For<IGroundItem>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            var inventory = Substitute.For<IInventoryContainer>();
            var firemakingDefinition = new FiremakingDto { ItemId = 1, RequiredLevel = 1, FireObjectId = 1, Experience = 1, Ticks = 1 };

            tinderboxItem.Id.Returns(FiremakingConstants.Tinderbox);
            logItem.ItemScript.Returns(itemScript);
            character.Inventory.Returns(inventory);
            inventory.GetInstanceSlot(logItem).Returns(0);
            _firemakingService.FindByLogId(logItem.Id).Returns(Task.FromResult<FiremakingDto?>(firemakingDefinition));
            itemScript.DropItem(logItem, character).Returns(true);
            character.ServiceProvider.Returns(serviceProvider);
            serviceProvider.GetService(typeof(IGroundItemService)).Returns(groundItemService);
            groundItemService.FindAllGroundItems(character.Location).Returns(new[] { groundItem });
            groundItem.ItemOnGround.Returns(logItem);

            var item1 = tinderboxFirst ? tinderboxItem : logItem;
            var item2 = tinderboxFirst ? logItem : tinderboxItem;

            // Act
            var result = _standardLog.UseItemOnItem(item1, item2, character);

            // Assert
            Assert.IsTrue(result, $"Test case: {displayName}");
            character.Received(1).QueueTask(Arg.Any<ITaskItem>());
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
            var expectedMessage = $"You need firemaking level of {firemakingDefinition.RequiredLevel} to light this log.";
            character.Received().SendChatMessage(expectedMessage);
        }

        [TestMethod]
        public void UseItemOnItem_WhenDropFails_ShouldReturnFalseAndSendMessage()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var logItem = Substitute.For<IItem>();
            var tinderboxItem = Substitute.For<IItem>();
            var itemScript = Substitute.For<IItemScript>();
            var inventory = Substitute.For<IInventoryContainer>();

            tinderboxItem.Id.Returns(FiremakingConstants.Tinderbox);
            logItem.ItemScript.Returns(itemScript);
            character.Inventory.Returns(inventory);
            inventory.GetInstanceSlot(logItem).Returns(0);
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
            var result = _standardLog.UseItemOnItem(tinderboxItem, logItem, character);

            // Assert
            Assert.IsFalse(result);
            character.Received().SendChatMessage("You can't drop the logs here to make a fire.");
        }

        [TestMethod]
        public async Task LightGroundLog_Success_ShouldQueueTaskAndSendMessage()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var groundItem = Substitute.For<IGroundItem>();
            var logItem = Substitute.For<IItem>();
            var statistics = Substitute.For<ICharacterStatistics>();
            var mapRegion = Substitute.For<IMapRegion>();
            var location = Substitute.For<ILocation>();
            var firemakingDefinition = new FiremakingDto { ItemId = 1, RequiredLevel = 1, FireObjectId = 1, Experience = 1, Ticks = 1 };
            var inventory = Substitute.For<IInventoryContainer>();

            groundItem.ItemOnGround.Returns(logItem);
            groundItem.Location.Returns(location);
            _firemakingService.FindByLogId(logItem.Id).Returns(Task.FromResult<FiremakingDto?>(firemakingDefinition));
            character.Statistics.Returns(statistics);
            statistics.GetSkillLevel(StatisticsConstants.Firemaking).Returns(99);
            _mapRegionService.GetMapRegion(Arg.Any<int>(), Arg.Any<int>(), false, true).Returns(mapRegion);
            mapRegion.FindStandardGameObject(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns((IGameObject)null);
            character.Inventory.Returns(inventory);
            inventory.GetById(FiremakingConstants.Tinderbox).Returns(Substitute.For<IItem>());

            // Act
            await _standardLog.LightGroundLog(character, groundItem);

            // Assert
            character.Received().FaceLocation(location);
            character.Received().QueueTask(Arg.Any<FiremakingTask>());
            character.Received().SendChatMessage("You attempt to light the logs.");
        }
    }
}
