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
            // tool + 2 bait + 1 free slot; the bait removed first frees a second slot, so one slot remains afterwards.
            var fixture = Create(capacity: 4, baitCount: 2, fishCount: 1, exhaustChance: 0.0);

            fixture.Task!.Tick();

            Assert.AreEqual(1, fixture.Count(BaitId));
            Assert.AreEqual(1, fixture.Count(FishId));
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

            inventory.Items.Add(ComposedTestContainer.CreateTestItem(ToolId));
            var baitPool = new List<IItem>();
            for (var i = 0; i < baitCount; i++)
            {
                var bait = ComposedTestContainer.CreateTestItem(BaitId);
                baitPool.Add(bait);
                inventory.Items.Add(bait);
            }

            // Test items compare by reference, so the builder hands back a bait instance that is still held.
            var idStep = Substitute.For<IItemId>();
            itemBuilder.Create().Returns(idStep);
            idStep.WithId(Arg.Any<int>()).Returns(call =>
            {
                var id = call.ArgAt<int>(0);
                var optional = Substitute.For<IItemOptional>();
                optional.Build().Returns(_ => id == BaitId
                    ? baitPool.FirstOrDefault(b => inventory.Items.GetSlotByItem(b) >= 0) ?? ComposedTestContainer.CreateTestItem(id)
                    : ComposedTestContainer.CreateTestItem(id));
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

        private sealed class Fixture
        {
            public required ComposedTestContainer Inventory { get; init; }
            public required ICharacterStatistics Statistics { get; init; }
            public bool IsLive { get; set; }
            public FishingTask? Task { get; set; }
            public ITaskItem? RespawnTask { get; set; }

            public int Count(int id) => Inventory.Items.Count(item => item != null && item.Id == id);
        }
    }
}
