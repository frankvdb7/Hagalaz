using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Skills.Woodcutting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hagalaz.Game.Scripts.Tests.Skills.Woodcutting
{
    [TestClass]
    public class WoodcuttingSkillTests
    {
        private Mock<IServiceProvider> _serviceProviderMock;
        private Mock<ICharacterStore> _characterStoreMock;
        private Mock<IRsTaskService> _rsTaskServiceMock;
        private Mock<IWoodcuttingService> _woodcuttingServiceMock;
        private Mock<ILootService> _lootServiceMock;
        private Mock<IGameObjectService> _gameObjectServiceMock;
        private Mock<IGameObjectBuilder> _gameObjectBuilderMock;

        private WoodcuttingSkillService _woodcuttingSkillService;

        [TestInitialize]
        public void Setup()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _characterStoreMock = new Mock<ICharacterStore>();
            _rsTaskServiceMock = new Mock<IRsTaskService>();
            _woodcuttingServiceMock = new Mock<IWoodcuttingService>();
            _lootServiceMock = new Mock<ILootService>();
            _gameObjectServiceMock = new Mock<IGameObjectService>();
            _gameObjectBuilderMock = new Mock<IGameObjectBuilder>();

            var serviceScopeMock = new Mock<IServiceScope>();
            serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);

            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            serviceScopeFactoryMock.Setup(s => s.CreateScope()).Returns(serviceScopeMock.Object);

            _serviceProviderMock.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactoryMock.Object);
            _serviceProviderMock.Setup(s => s.GetService(typeof(IWoodcuttingService))).Returns(_woodcuttingServiceMock.Object);
            _serviceProviderMock.Setup(s => s.GetService(typeof(ILootService))).Returns(_lootServiceMock.Object);
            _serviceProviderMock.Setup(s => s.GetService(typeof(IGameObjectService))).Returns(_gameObjectServiceMock.Object);
            _serviceProviderMock.Setup(s => s.GetService(typeof(IGameObjectBuilder))).Returns(_gameObjectBuilderMock.Object);

            _woodcuttingSkillService = new WoodcuttingSkillService(
                _serviceProviderMock.Object,
                _characterStoreMock.Object,
                _rsTaskServiceMock.Object);
        }

        private Mock<ICharacter> SetupCharacter(int woodcuttingLevel, int hatchetId = 0)
        {
            var characterMock = new Mock<ICharacter>();
            var statisticsMock = new Mock<ICharacterStatistics>();
            var inventoryMock = new Mock<IInventoryContainer>();
            var equipmentMock = new Mock<IEquipmentContainer>();

            characterMock.Setup(c => c.Statistics).Returns(statisticsMock.Object);
            characterMock.Setup(c => c.Inventory).Returns(inventoryMock.Object);
            characterMock.Setup(c => c.Equipment).Returns(equipmentMock.Object);
            characterMock.Setup(c => c.IsDestroyed).Returns(false);

            statisticsMock.Setup(s => s.GetSkillLevel(StatisticsConstants.Woodcutting)).Returns(woodcuttingLevel);

            if (hatchetId > 0)
            {
                var hatchetItemMock = new Mock<IItem>();
                hatchetItemMock.Setup(i => i.Id).Returns(hatchetId);
                inventoryMock.Setup(i => i.GetById(hatchetId)).Returns(hatchetItemMock.Object);
            }

            inventoryMock.Setup(i => i.FreeSlots).Returns(28);

            return characterMock;
        }

        private Mock<IGameObject> SetupTree(int treeId, int requiredLevel)
        {
            var treeMock = new Mock<IGameObject>();
            treeMock.Setup(t => t.Id).Returns(treeId);
            treeMock.Setup(t => t.IsDestroyed).Returns(false);
            treeMock.Setup(t => t.IsDisabled).Returns(false);

            var logDto = new LogDto
            {
                ItemID = 1511,
                RequiredLevel = requiredLevel,
                FallChance = 0.1,
                BaseHarvestChance = 0.8,
                WoodcuttingExperience = 25.0,
                RespawnTime = 10
            };
            _woodcuttingServiceMock.Setup(s => s.FindLogByTreeId(treeId)).ReturnsAsync(logDto);

            var treeDto = new TreeDto { Id = treeId, StumpId = treeId + 1 };
            _woodcuttingServiceMock.Setup(s => s.FindTreeById(treeId)).ReturnsAsync(treeDto);

            return treeMock;
        }

        [TestMethod]
        public async Task StartCutting_Fails_When_Level_Is_Too_Low()
        {
            // Arrange
            var characterMock = SetupCharacter(1);
            var treeMock = SetupTree(1276, 99);

            // Act
            var result = await _woodcuttingSkillService.StartCutting(characterMock.Object, treeMock.Object);

            // Assert
            result.Should().BeTrue();
            characterMock.Verify(c => c.SendChatMessage("You must have a woodcutting level of 99 or higher to cut this tree.", It.IsAny<ChatMessageType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _rsTaskServiceMock.Verify(s => s.Schedule(It.IsAny<WoodcuttingTask>()), Times.Never);
        }

        [TestMethod]
        public async Task StartCutting_Fails_When_No_Hatchet_Is_Found()
        {
            // Arrange
            var characterMock = SetupCharacter(99);
            var treeMock = SetupTree(1276, 1);

            _woodcuttingServiceMock.Setup(s => s.FindAllHatchets()).ReturnsAsync(new List<HatchetDto>());

            // Act
            await _woodcuttingSkillService.StartCutting(characterMock.Object, treeMock.Object);

            // Assert
            characterMock.Verify(c => c.SendChatMessage(WoodcuttingSkillService.NoHatchetFound, It.IsAny<ChatMessageType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _rsTaskServiceMock.Verify(s => s.Schedule(It.IsAny<WoodcuttingTask>()), Times.Never);
        }

        [TestMethod]
        public async Task StartCutting_Fails_When_Inventory_Is_Full()
        {
            // Arrange
            var hatchetId = 1351; // Bronze hatchet
            var characterMock = SetupCharacter(99, hatchetId);
            characterMock.Setup(c => c.Inventory.FreeSlots).Returns(0);

            var treeMock = SetupTree(1276, 1);

            var hatchets = new List<HatchetDto>
            {
                new()
                {
                    ItemId = hatchetId,
                    RequiredLevel = 1,
                    Type = HatchetType.Bronze,
                    ChopAnimationId = 879,
                    CanoeAnimationId = 1,
                    BaseHarvestChance = 0.1
                }
            };
            _woodcuttingServiceMock.Setup(s => s.FindAllHatchets()).ReturnsAsync(hatchets);

            // Act
            await _woodcuttingSkillService.StartCutting(characterMock.Object, treeMock.Object);

            // Assert
            characterMock.Verify(c => c.SendChatMessage("Not enough space in your inventory.", It.IsAny<ChatMessageType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _rsTaskServiceMock.Verify(s => s.Schedule(It.IsAny<WoodcuttingTask>()), Times.Never);
        }

        [TestMethod]
        public async Task StartCutting_Succeeds_And_Queues_Task()
        {
            // Arrange
            var hatchetId = 1351; // Bronze hatchet
            var characterMock = SetupCharacter(99, hatchetId);
            var treeMock = SetupTree(1276, 1);

            var hatchets = new List<HatchetDto>
            {
                new()
                {
                    ItemId = hatchetId,
                    RequiredLevel = 1,
                    Type = HatchetType.Bronze,
                    ChopAnimationId = 879,
                    CanoeAnimationId = 1,
                    BaseHarvestChance = 0.1
                }
            };
            _woodcuttingServiceMock.Setup(s => s.FindAllHatchets()).ReturnsAsync(hatchets);

            // Act
            await _woodcuttingSkillService.StartCutting(characterMock.Object, treeMock.Object);

            // Assert
            characterMock.Verify(c => c.SendChatMessage(WoodcuttingSkillService.SwingAxe, It.IsAny<ChatMessageType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            characterMock.Verify(c => c.QueueTask(It.IsAny<WoodcuttingTask>()), Times.Once);
            characterMock.Verify(c => c.QueueAnimation(It.Is<IAnimation>(a => a.Id == 879)), Times.Once);
        }
    }
}
