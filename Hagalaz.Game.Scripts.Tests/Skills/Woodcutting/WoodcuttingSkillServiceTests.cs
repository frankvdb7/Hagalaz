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
using Hagalaz.Game.Abstractions.Collections;
using Microsoft.Extensions.DependencyInjection;
using System;
using Hagalaz.Game.Resources;
using NSubstitute;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Common;
using Hagalaz.Game.Extensions;
using System.Linq;

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

        [TestInitialize]
        public void TestInitialize()
        {
            _serviceProvider = Substitute.For<IServiceProvider>();
            _characterStore = Substitute.For<ICharacterStore>();
            _rsTaskService = Substitute.For<IRsTaskService>();
            _woodcuttingService = Substitute.For<IWoodcuttingService>();
            _lootService = Substitute.For<ILootService>();
            _lootGenerator = Substitute.For<ILootGenerator>();

            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(_serviceProvider);
            scopeFactory.CreateScope().Returns(scope);

            _serviceProvider.GetService(typeof(IWoodcuttingService)).Returns(_woodcuttingService);
            _serviceProvider.GetService(typeof(ILootService)).Returns(_lootService);
            _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
            _serviceProvider.GetService(typeof(ILootGenerator)).Returns(_lootGenerator);


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

        /*
        [TestMethod]
        public async Task Cut_ShouldNotRewardExperience_WhenNoLogsAreCut()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            character.ServiceProvider.Returns(_serviceProvider);
            var inventory = Substitute.For<IInventoryContainer>();
            inventory.FreeSlots.Returns(1);
            character.Inventory.Returns(inventory);
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
            lootTable.Entries.Returns(new List<ILootObject>());
            _lootService.FindGameObjectLootTable(gameObjectDefinition.LootTableId).Returns(Task.FromResult<ILootTable?>(lootTable));
            _lootGenerator.GenerateLoot<ILootItem>(Arg.Any<LootParams>()).Returns(new List<LootResult<ILootItem>>());

            WoodcuttingTask? woodcuttingTask = null;
            character.When(x => x.QueueTask(Arg.Any<WoodcuttingTask>())).Do(x => {
                woodcuttingTask = x.Arg<WoodcuttingTask>();
            });

            // Act
            await _woodcuttingSkillService.StartCutting(character, tree);

            if(woodcuttingTask != null)
            {
                //This will execute the task
                while(!woodcuttingTask.IsCompleted && !woodcuttingTask.IsCancelled && !woodcuttingTask.IsFaulted)
                {
                    woodcuttingTask.Tick();
                }
            }

            // Assert
            character.DidNotReceive().SendChatMessage("You get some logs.", Arg.Any<ChatMessageType>(), null);
            character.Statistics.DidNotReceive().AddExperience(StatisticsConstants.Woodcutting, Arg.Any<double>());
        }
        */
    }
}
