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

        [TestInitialize]
        public void TestInitialize()
        {
            _serviceProvider = Substitute.For<IServiceProvider>();
            _characterStore = Substitute.For<ICharacterStore>();
            _rsTaskService = Substitute.For<IRsTaskService>();
            _woodcuttingService = Substitute.For<IWoodcuttingService>();
            _lootService = Substitute.For<ILootService>();

            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(_serviceProvider);
            scopeFactory.CreateScope().Returns(scope);

            _serviceProvider.GetService(typeof(IWoodcuttingService)).Returns(_woodcuttingService);
            _serviceProvider.GetService(typeof(ILootService)).Returns(_lootService);
            _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);


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
            _woodcuttingService.FindLogByTreeId(tree.Id).Returns(Task.FromResult(log));
            var treeDto = new TreeDto { Id = 1, StumpId = 2 };
            _woodcuttingService.FindTreeById(tree.Id).Returns(Task.FromResult(treeDto));

            // Act
            await _woodcuttingSkillService.StartCutting(character, tree);

            // Assert
            character.Received().SendChatMessage(GameStrings.InventoryFull, Arg.Any<ChatMessageType>(), null);
            character.DidNotReceive().QueueTask(Arg.Any<RsTask>());
        }
    }
}
