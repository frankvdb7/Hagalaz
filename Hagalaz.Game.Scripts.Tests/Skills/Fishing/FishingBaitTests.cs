using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Skills.Fishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Fishing
{
    [TestClass]
    public sealed class FishingBaitTests
    {
        private const int ToolId = 1;
        private const int BaitId = 2;
        private const int FishId = 3;
        private const double Experience = 10.0;

        [TestMethod]
        public void Catch_ConsumesOneBaitPerFish()
        {
            var fixture = Create(capacity: 6, baitCount: 2, fishCount: 1, exhaustChance: 0.0);

            fixture.Task!.Tick();

            Assert.AreEqual(1, fixture.Count(BaitId));
            Assert.AreEqual(1, fixture.Count(FishId));
            fixture.Statistics.Received(1).AddExperience(StatisticsConstants.Fishing, Experience);
            Assert.IsFalse(fixture.Task.IsCancelled);
        }

        [TestMethod]
        public void Catch_WhenSpotExhausts_StillConsumesBait()
        {
            var fixture = Create(capacity: 6, baitCount: 2, fishCount: 1, exhaustChance: 1.0);

            fixture.Task!.Tick();

            Assert.AreEqual(1, fixture.Count(BaitId));
            Assert.AreEqual(1, fixture.Count(FishId));
            Assert.IsNotNull(fixture.RespawnTask);
        }

        [TestMethod]
        public void Catch_IntoLastFreeSlot_ConsumesBaitAndStopsFishing()
        {
            // tool + bait stack + exactly one free slot: the fish fills the inventory.
            var fixture = Create(capacity: 3, baitCount: 2, fishCount: 1, exhaustChance: 0.0);

            fixture.Task!.Tick();

            Assert.AreEqual(0, fixture.Inventory.Items.FreeSlots);
            Assert.AreEqual(1, fixture.Count(BaitId));
            Assert.AreEqual(1, fixture.Count(FishId));
            fixture.Statistics.Received(1).AddExperience(StatisticsConstants.Fishing, Experience);
            Assert.IsTrue(fixture.Task.IsCancelled);
        }

        [TestMethod]
        public void MultipleLoot_WhenFishCannotBeInserted_RefundsBaitAndAwardsNoExperience()
        {
            // One free slot: the first fish fits, the second cannot be inserted and its bait is restored.
            var fixture = Create(capacity: 3, baitCount: 3, fishCount: 2, exhaustChance: 0.0);

            fixture.Task!.Tick();

            Assert.AreEqual(2, fixture.Count(BaitId));
            Assert.AreEqual(1, fixture.Count(FishId));
            fixture.Statistics.Received(1).AddExperience(StatisticsConstants.Fishing, Experience);
            Assert.IsTrue(fixture.Task.IsCancelled);
        }

        [TestMethod]
        public void Catch_WithExactlyOneBait_ConsumesItAndStopsWithoutFreeCatch()
        {
            var fixture = Create(capacity: 6, baitCount: 1, fishCount: 1, exhaustChance: 0.0);

            fixture.Task!.Tick();

            Assert.AreEqual(0, fixture.Count(BaitId));
            Assert.AreEqual(1, fixture.Count(FishId));
            Assert.IsTrue(fixture.Task.IsCancelled);
        }

        [TestMethod]
        public void MultipleLoot_OneBait_SingleBaitNeverFundsMultipleFish()
        {
            var fixture = Create(capacity: 8, baitCount: 1, fishCount: 3, exhaustChance: 0.0);

            fixture.Task!.Tick();

            Assert.AreEqual(0, fixture.Count(BaitId));
            Assert.AreEqual(1, fixture.Count(FishId));
            fixture.Statistics.Received(1).AddExperience(StatisticsConstants.Fishing, Experience);
        }

        [TestMethod]
        public void MultipleLoot_EnoughBait_ConsumesOneBaitForEachFish()
        {
            var fixture = Create(capacity: 8, baitCount: 3, fishCount: 2, exhaustChance: 1.0);

            fixture.Task!.Tick();

            Assert.AreEqual(1, fixture.Count(BaitId));
            Assert.AreEqual(2, fixture.Count(FishId));
            fixture.Statistics.Received(2).AddExperience(StatisticsConstants.Fishing, Experience);
        }

        [TestMethod]
        public void FailedCatch_NoLoot_ConsumesNoBait()
        {
            var fixture = Create(capacity: 6, baitCount: 2, fishCount: 0, exhaustChance: 0.0);

            fixture.Task!.Tick();

            Assert.AreEqual(2, fixture.Count(BaitId));
            Assert.AreEqual(0, fixture.Count(FishId));
            Assert.IsFalse(fixture.Task.IsCancelled);
        }

        [TestMethod]
        public void Catch_WhenSpotDisappears_ConsumesNoBaitAndAwardsNothing()
        {
            var fixture = Create(capacity: 6, baitCount: 2, fishCount: 1, exhaustChance: 0.0);
            fixture.IsLive = false;

            fixture.Task!.Tick();

            Assert.AreEqual(2, fixture.Count(BaitId));
            Assert.AreEqual(0, fixture.Count(FishId));
            fixture.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
        }

        private static Fixture Create(int capacity, int baitCount, int fishCount, double exhaustChance)
        {
            var character = Substitute.For<ICharacter>();
            var inventory = new ComposedTestContainer(capacity);
            var statistics = Substitute.For<ICharacterStatistics>();
            var characterProvider = Substitute.For<IServiceProvider>();
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            var scope = Substitute.For<IServiceScope>();
            var scopedProvider = Substitute.For<IServiceProvider>();
            var entityService = Substitute.For<IEntityService>();
            var itemService = Substitute.For<IItemService>();
            var itemBuilder = Substitute.For<IItemBuilder>();
            var lootGenerator = Substitute.For<ILootGenerator>();
            var taskService = Substitute.For<IRsTaskService>();
            var spot = Substitute.For<INpc>();
            var appearance = Substitute.For<INpcAppearance>();
            var viewport = Substitute.For<IViewport>();
            var movement = Substitute.For<IMovement>();
            var handle = new EntityHandle<ICreature>(5, 1);
            var fixture = new Fixture { Inventory = inventory, Statistics = statistics, IsLive = true };

            spot.Handle.Returns(handle);
            spot.Appearance.Returns(appearance);
            spot.Movement.Returns(movement);
            appearance.Visible.Returns(true);
            viewport.VisibleCreatures.Returns(new List<ICreature> { spot });
            character.Viewport.Returns(viewport);
            character.Inventory.Returns(inventory);
            character.Statistics.Returns(statistics);
            character.ServiceProvider.Returns(characterProvider);
            statistics.GetSkillLevel(StatisticsConstants.Fishing).Returns(99);

            var definition = Substitute.For<IItemDefinition>();
            definition.Name.Returns("fishing rod");
            itemService.FindItemDefinitionById(Arg.Any<int>()).Returns(definition);

            inventory.Items.Add(new TestItem(ToolId, 1, stackable: false));
            if (baitCount > 0)
            {
                inventory.Items.Add(new TestItem(BaitId, baitCount, stackable: true));
            }

            var idStep = Substitute.For<IItemId>();
            itemBuilder.Create().Returns(idStep);
            idStep.WithId(Arg.Any<int>()).Returns(call =>
            {
                var id = call.ArgAt<int>(0);
                var optional = Substitute.For<IItemOptional>();
                optional.Build().Returns(_ => new TestItem(id, 1, stackable: id == BaitId));
                return optional;
            });

            characterProvider.GetService(typeof(IEntityService)).Returns(entityService);
            characterProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
            characterProvider.GetService(typeof(ILootGenerator)).Returns(lootGenerator);
            scope.ServiceProvider.Returns(scopedProvider);
            scopeFactory.CreateScope().Returns(scope);
            scopedProvider.GetService(typeof(IItemService)).Returns(itemService);

            var loot = Enumerable.Range(0, fishCount).Select(_ =>
            {
                var item = Substitute.For<IFishingLoot>();
                item.Id.Returns(FishId);
                item.FishingExperience.Returns(Experience);
                return new LootResult<IFishingLoot>(item, 1);
            }).ToArray();
            lootGenerator.GenerateLoot<IFishingLoot>(Arg.Any<CharacterLootParams>()).Returns(loot);

            entityService.TryResolve<ICreature>(handle, out Arg.Any<ICreature>()).Returns(call =>
            {
                if (!fixture.IsLive)
                {
                    call[1] = null;
                    return false;
                }

                call[1] = spot;
                return true;
            });

            var table = Substitute.For<IFishingSpotTable>();
            table.MinimumLevel.Returns(1);
            table.BaseCatchChance.Returns(1.0);
            table.ExhaustChance.Returns(exhaustChance);
            table.RespawnTime.Returns(0.0);
            table.BaitId.Returns(BaitId);
            table.RequiredTool.Returns(new FishingToolDto { ItemId = ToolId, FishAnimationId = 1, CastAnimationId = 0 });

            character.QueueTask(Arg.Do<ITaskItem>(task => fixture.Task = task as FishingTask))
                .Returns(Substitute.For<IRsTaskHandle>());
            taskService.Schedule(Arg.Do<ITaskItem>(task => fixture.RespawnTask = task));

            var service = new FishingSkillService(characterProvider, taskService, itemBuilder);
            Assert.IsTrue(service.TryFish(character, handle, table, characterCount: 0));
            Assert.IsNotNull(fixture.Task);
            return fixture;
        }

        private sealed class TestItem(int id, int count, bool stackable) : IItem
        {
            public int Id { get; } = id;
            public string Name => "item";
            public long[] ExtraData => [];
            public int Count { get; set; } = count;
            public IItemDefinition ItemDefinition { get; } = CreateDefinition(stackable);
            public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
            public IItemScript ItemScript { get; } = CreateScript(stackable);
            public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
            public IItem Clone() => new TestItem(Id, Count, ItemDefinition.Stackable);
            public IItem Clone(int newCount) => new TestItem(Id, newCount, ItemDefinition.Stackable);
            public bool Equals(IItem otherItem, bool ignoreCount = true) =>
                otherItem is not null && Id == otherItem.Id && (ignoreCount || Count == otherItem.Count);
            public string? SerializeExtraData() => null;

            private static IItemScript CreateScript(bool stackable)
            {
                var script = Substitute.For<IItemScript>();
                script.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(stackable);
                return script;
            }

            private static IItemDefinition CreateDefinition(bool stackable)
            {
                var definition = Substitute.For<IItemDefinition>();
                definition.Stackable.Returns(stackable);
                definition.Name.Returns("item");
                return definition;
            }
        }

        private sealed class Fixture
        {
            public required ComposedTestContainer Inventory { get; init; }
            public required ICharacterStatistics Statistics { get; init; }
            public bool IsLive { get; set; }
            public FishingTask? Task { get; set; }
            public ITaskItem? RespawnTask { get; set; }

            public int Count(int id) => Inventory.Items.Where(item => item != null && item.Id == id).Sum(item => item!.Count);
        }
    }
}
