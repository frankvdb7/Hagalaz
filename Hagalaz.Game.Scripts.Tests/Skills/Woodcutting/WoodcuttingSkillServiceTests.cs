using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Skills.Woodcutting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services.Model;
using System.Threading.Tasks;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Items;
using Microsoft.Extensions.DependencyInjection;
using System;
using Hagalaz.Game.Resources;
using NSubstitute;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Common;
using System.Reflection;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Tests.Skills.Woodcutting
{
    [TestClass]
    public class WoodcuttingSkillServiceTests
    {
        private IServiceProvider _serviceProvider;
        private ICharacterStore _characterStore;
        private IRsTaskService _rsTaskService;
        private IWoodcuttingService _woodcuttingService;
        private ILootService _lootService;
        private WoodcuttingSkillService _woodcuttingSkillService;
        private ILootGenerator _lootGenerator;
        private IItemBuilder _itemBuilder;
        private IGroundItemBuilder _groundItemBuilder;
        private IGameObjectService _gameObjectService;
        private IGameObjectBuilder _gameObjectBuilder;
        private IGameObjectId _gameObjectId;
        private IGameObjectLocation _gameObjectLocation;
        private IGameObjectOptional _gameObjectOptional;


        [TestInitialize]
        public void TestInitialize()
        {
            _serviceProvider = Substitute.For<IServiceProvider>();
            _characterStore = Substitute.For<ICharacterStore>();
            _rsTaskService = Substitute.For<IRsTaskService>();
            _woodcuttingService = Substitute.For<IWoodcuttingService>();
            _lootService = Substitute.For<ILootService>();
            _lootGenerator = Substitute.For<ILootGenerator>();
            _itemBuilder = Substitute.For<IItemBuilder>();
            _groundItemBuilder = Substitute.For<IGroundItemBuilder>();
            _gameObjectService = Substitute.For<IGameObjectService>();
            _gameObjectBuilder = Substitute.For<IGameObjectBuilder>();
            _gameObjectId = Substitute.For<IGameObjectId>();
            _gameObjectLocation = Substitute.For<IGameObjectLocation>();
            _gameObjectOptional = Substitute.For<IGameObjectOptional>();

            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(_serviceProvider);
            scopeFactory.CreateScope().Returns(scope);

            _serviceProvider.GetService(typeof(IWoodcuttingService)).Returns(_woodcuttingService);
            _serviceProvider.GetService(typeof(ILootService)).Returns(_lootService);
            _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
            _serviceProvider.GetService(typeof(ILootGenerator)).Returns(_lootGenerator);
            _serviceProvider.GetService(typeof(IItemBuilder)).Returns(_itemBuilder);
            _serviceProvider.GetService(typeof(IGroundItemBuilder)).Returns(_groundItemBuilder);
            _serviceProvider.GetService(typeof(IGameObjectService)).Returns(_gameObjectService);
            _serviceProvider.GetService(typeof(IGameObjectBuilder)).Returns(_gameObjectBuilder);

            _gameObjectBuilder.Create().Returns(_gameObjectId);
            _gameObjectId.WithId(Arg.Any<int>()).Returns(_gameObjectLocation);
            _gameObjectLocation.WithLocation(Arg.Any<ILocation>()).Returns(_gameObjectOptional);
            _gameObjectOptional.WithRotation(Arg.Any<int>()).Returns(_gameObjectOptional);
            _gameObjectOptional.WithShape(Arg.Any<ShapeType>()).Returns(_gameObjectOptional);
            _gameObjectOptional.Build().Returns(Substitute.For<IGameObject>());


            _woodcuttingSkillService = new WoodcuttingSkillService(
                _serviceProvider,
                _characterStore,
                _rsTaskService
            );
        }

        [TestMethod]
        public async Task StartCutting_WhenInventoryIsFull_ShouldNotAddLogs()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var inventory = Substitute.For<IInventoryContainer>();
            inventory.FreeSlots.Returns(0);
            character.Inventory.Returns(inventory);
            character.Statistics.GetSkillLevel(Arg.Any<int>()).Returns(99);

            var hatchet = new HatchetDto { ItemId = 1, RequiredLevel = 1, Type = HatchetType.Bronze, ChopAnimationId = 1, CanoeAnimationId = 1, BaseHarvestChance = 0.1 };
            _woodcuttingService.FindAllHatchets().Returns(Task.FromResult((IReadOnlyList<HatchetDto>)new List<HatchetDto> { hatchet }));
            character.Equipment.GetById(hatchet.ItemId).Returns(Substitute.For<IItem>());

            var tree = Substitute.For<IGameObject>();
            tree.Id.Returns(1);
            var log = new LogDto { ItemID = 2, RequiredLevel = 1, RespawnTime = 1, FallChance = 0.1, BaseHarvestChance = 0.1, WoodcuttingExperience = 10 };
            _woodcuttingService.FindLogByTreeId(tree.Id).Returns(Task.FromResult<LogDto?>(log));
            var treeDto = new TreeDto { Id = 1, StumpId = 2 };
            _woodcuttingService.FindTreeById(tree.Id).Returns(Task.FromResult<TreeDto?>(treeDto));

            // Act
            await _woodcuttingSkillService.StartCutting(character, tree);

            // Assert
            character.Received().SendChatMessage(GameStrings.InventoryFull, Arg.Any<ChatMessageType>(), null);
            character.DidNotReceive().QueueTask(Arg.Any<RsTask>());
        }

        [TestMethod]
        public async Task Cut_ShouldNotRewardExperience_WhenNoLogsAreCut()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            character.ServiceProvider.Returns(_serviceProvider);

            var inventory = Substitute.For<IInventoryContainer>();
            inventory.FreeSlots.Returns(1);
            character.Inventory.Returns(inventory);

            // Since TryAddLoot is an extension method, we can't mock it directly.
            // Instead, we mock its dependencies to control its output.
            // Here, we configure the loot generator to return an empty list, so no items are generated.
            _lootGenerator.GenerateLoot<ILootItem>(Arg.Any<CharacterLootParams>()).Returns(new List<LootResult<ILootItem>>());

            character.Statistics.GetSkillLevel(Arg.Any<int>()).Returns(99);

            var hatchet = new HatchetDto { ItemId = 1, RequiredLevel = 1, Type = HatchetType.Bronze, ChopAnimationId = 1, CanoeAnimationId = 1, BaseHarvestChance = 0.1 };
            _woodcuttingService.FindAllHatchets().Returns(Task.FromResult((IReadOnlyList<HatchetDto>)new List<HatchetDto> { hatchet }));
            character.Equipment.GetById(hatchet.ItemId).Returns(Substitute.For<IItem>());

            var tree = Substitute.For<IGameObject>();
            tree.Id.Returns(1);
            var log = new LogDto { ItemID = 2, RequiredLevel = 1, RespawnTime = 1, FallChance = 1.0, BaseHarvestChance = 1.0, WoodcuttingExperience = 10 };
            _woodcuttingService.FindLogByTreeId(tree.Id).Returns(Task.FromResult<LogDto?>(log));
            var treeDto = new TreeDto { Id = 1, StumpId = 2 };
            _woodcuttingService.FindTreeById(tree.Id).Returns(Task.FromResult<TreeDto?>(treeDto));

            var gameObjectDefinition = Substitute.For<IGameObjectDefinition>();
            gameObjectDefinition.LootTableId.Returns(1);
            tree.Definition.Returns(gameObjectDefinition);

            var lootTable = Substitute.For<ILootTable>();
            _lootService.FindGameObjectLootTable(gameObjectDefinition.LootTableId).Returns(Task.FromResult<ILootTable?>(lootTable));

            WoodcuttingTask? woodcuttingTask = null;
            character.When(x => x.QueueTask(Arg.Any<WoodcuttingTask>())).Do(x => {
                woodcuttingTask = x.Arg<WoodcuttingTask>();
            });

            // Act
            await _woodcuttingSkillService.StartCutting(character, tree);

            Assert.IsNotNull(woodcuttingTask);

            // Invoke the callback directly
            await woodcuttingTask._finishCallback();

            // Assert
            character.DidNotReceive().SendChatMessage(WoodcuttingSkillService.LogsReceived, Arg.Any<ChatMessageType>(), null);
            character.Statistics.DidNotReceive().AddExperience(StatisticsConstants.Woodcutting, Arg.Any<double>());
        }
    }
}
