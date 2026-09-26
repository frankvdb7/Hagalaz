using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Logic.Skills;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Skills.Fishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Fishing
{
    [TestClass]
    public sealed class FishingIdentityTests
    {
        [TestMethod]
        public void FishingTask_StaleTarget_CancelsBeforeRewardOrAnimation()
        {
            var performer = Substitute.For<ICharacter>();
            var entityService = Substitute.For<IEntityService>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            var handle = new EntityHandle<ICreature>(1, 1);
            serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
            performer.ServiceProvider.Returns(serviceProvider);
            entityService.TryResolve<ICreature>(handle, out Arg.Any<ICreature>()).Returns(false);

            var callbackCalls = 0;
            var task = new FishingTask(
                performer,
                _ =>
                {
                    callbackCalls++;
                    return true;
                },
                chance: 1.0,
                handle,
                animId: 1);

            task.Tick();

            Assert.IsTrue(task.IsCancelled);
            Assert.AreEqual(0, callbackCalls);
            performer.DidNotReceive().QueueAnimation(Arg.Any<IAnimation>());
        }

        [TestMethod]
        public void FishingTask_StaleHandleDoesNotUseReplacement()
        {
            var performer = Substitute.For<ICharacter>();
            var entityService = Substitute.For<IEntityService>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            var staleHandle = new EntityHandle<ICreature>(2, 1);
            var replacement = Substitute.For<INpc>();
            replacement.Handle.Returns(new EntityHandle<ICreature>(2, 2));
            serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
            performer.ServiceProvider.Returns(serviceProvider);
            entityService.TryResolve<ICreature>(staleHandle, out Arg.Any<ICreature>()).Returns(false);

            var callbackCalls = 0;
            var task = new FishingTask(
                performer,
                target =>
                {
                    callbackCalls++;
                    Assert.AreSame(replacement, target);
                    return true;
                },
                chance: 1.0,
                staleHandle,
                animId: 1);

            task.Tick();

            Assert.IsTrue(task.IsCancelled);
            Assert.AreEqual(0, callbackCalls);
            performer.DidNotReceive().QueueAnimation(Arg.Any<IAnimation>());
        }

        [TestMethod]
        public async Task FishingSpot_WhenTargetBecomesStaleDuringSetup_DoesNotStartFishing()
        {
            var owner = Substitute.For<INpc>();
            var ownerAppearance = Substitute.For<INpcAppearance>();
            var clicker = Substitute.For<ICharacter>();
            var entityService = Substitute.For<IEntityService>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            var fishingService = Substitute.For<IFishingService>();
            var fishingSkillService = Substitute.For<IFishingSkillService>();
            var characterStore = Substitute.For<ICharacterStore>();
            var npcService = Substitute.For<INpcService>();
            var pathFinder = Substitute.For<ISimplePathFinder>();
            var widgetScriptActivator = Substitute.For<IWidgetScriptActivator>();
            var handle = new EntityHandle<ICreature>(3, 1);
            var lookup = new TaskCompletionSource<IFishingSpotTable?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var operation = (Func<CancellationToken, Task>?)null;
            var isLive = true;

            owner.Handle.Returns(handle);
            owner.Appearance.Returns(ownerAppearance);
            ownerAppearance.CompositeID.Returns(123);
            ownerAppearance.Visible.Returns(true);
            serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
            clicker.ServiceProvider.Returns(serviceProvider);
            entityService.TryResolve<ICreature>(handle, out Arg.Any<ICreature>()).Returns(callInfo =>
            {
                if (!isLive)
                {
                    callInfo[1] = null;
                    return false;
                }

                callInfo[1] = owner;
                return true;
            });
            fishingService.FindSpotByNpcIdClickType(123, NpcClickType.Option1Click).Returns(lookup.Task);
            clicker.RegisterEventHandler<CreatureInterruptedEvent>(Arg.Any<EventHappened<CreatureInterruptedEvent>>())
                .Returns(Substitute.For<EventHappened>());
            clicker.QueueTask(Arg.Do<Func<CancellationToken, Task>>(value => operation = value))
                .Returns(Substitute.For<IRsTaskHandle>());

            var script = new FishingSpot(
                owner,
                fishingService,
                fishingSkillService,
                characterStore,
                npcService,
                pathFinder,
                widgetScriptActivator);

            script.OnCharacterClickPerform(clicker, NpcClickType.Option1Click);
            Assert.IsNotNull(operation);

            var startup = operation!(CancellationToken.None);
            Assert.IsFalse(startup.IsCompleted);

            isLive = false;
            lookup.SetResult(Substitute.For<IFishingSpotTable>());
            await startup;

            fishingSkillService.DidNotReceive().TryFish(
                clicker,
                Arg.Any<EntityHandle<ICreature>>(),
                Arg.Any<IFishingSpotTable>(),
                Arg.Any<int>());
            clicker.DidNotReceive().QueueTask(Arg.Any<FishingTask>());
        }

        [TestMethod]
        public void Fishing_WhenTargetBecomesStaleBeforeRespawn_DoesNotRestoreIt()
        {
            var fixture = CreateFishingFixture();
            Assert.IsTrue(fixture.FishingSkillService.TryFish(
                fixture.Character,
                fixture.FishingSpotHandle,
                fixture.Table,
                characterCount: 0));

            Assert.IsNotNull(fixture.FishingTask);
            fixture.FishingTask!.Tick();
            Assert.IsNotNull(fixture.RespawnTask);

            fixture.IsLive = false;
            fixture.RespawnTask!.Tick();

            fixture.OriginalAppearance.Received(1).Visible = false;
            fixture.OriginalAppearance.DidNotReceive().Visible = true;
            fixture.TimerResolvedAppearance.DidNotReceive().Visible = true;
        }

        [TestMethod]
        public void Fishing_WhenOriginalTargetStillResolves_RespawnsExactResolvedTarget()
        {
            var fixture = CreateFishingFixture();
            Assert.IsTrue(fixture.FishingSkillService.TryFish(
                fixture.Character,
                fixture.FishingSpotHandle,
                fixture.Table,
                characterCount: 0));

            Assert.IsNotNull(fixture.FishingTask);
            fixture.FishingTask!.Tick();
            Assert.IsNotNull(fixture.RespawnTask);

            fixture.CurrentCreature = fixture.TimerResolvedSpot;
            fixture.RespawnTask!.Tick();

            fixture.OriginalAppearance.Received(1).Visible = false;
            fixture.TimerResolvedAppearance.Received(1).Visible = true;
            fixture.OriginalAppearance.DidNotReceive().Visible = true;
        }

        private static FishingFixture CreateFishingFixture()
        {
            var character = Substitute.For<ICharacter>();
            var inventory = Substitute.For<IInventoryContainer>();
            var statistics = Substitute.For<ICharacterStatistics>();
            var characterProvider = Substitute.For<IServiceProvider>();
            var scopedProvider = Substitute.For<IServiceProvider>();
            var scopeFactory = Substitute.For<IServiceScopeFactory>();
            var scope = Substitute.For<IServiceScope>();
            var entityService = Substitute.For<IEntityService>();
            var itemService = Substitute.For<IItemService>();
            var itemBuilder = Substitute.For<IItemBuilder>();
            var lootGenerator = Substitute.For<ILootGenerator>();
            var taskService = Substitute.For<IRsTaskService>();
            var originalSpot = Substitute.For<INpc>();
            var timerResolvedSpot = Substitute.For<INpc>();
            var originalAppearance = Substitute.For<INpcAppearance>();
            var timerResolvedAppearance = Substitute.For<INpcAppearance>();
            var viewport = Substitute.For<IViewport>();
            var movement = Substitute.For<IMovement>();
            var handle = new EntityHandle<ICreature>(4, 1);
            var fixture = new FishingFixture
            {
                Character = character,
                EntityService = entityService,
                FishingSkillService = null!,
                FishingSpotHandle = handle,
                OriginalAppearance = originalAppearance,
                TimerResolvedAppearance = timerResolvedAppearance,
                TimerResolvedSpot = timerResolvedSpot,
                OriginalSpot = originalSpot,
            };

            originalSpot.Handle.Returns(handle);
            originalSpot.Appearance.Returns(originalAppearance);
            originalSpot.Movement.Returns(movement);
            timerResolvedSpot.Handle.Returns(handle);
            timerResolvedSpot.Appearance.Returns(timerResolvedAppearance);
            originalAppearance.Visible.Returns(true);
            timerResolvedAppearance.Visible.Returns(false);
            viewport.VisibleCreatures.Returns(new List<ICreature> { originalSpot });
            character.Viewport.Returns(viewport);
            character.Inventory.Returns(inventory);
            character.Statistics.Returns(statistics);
            character.ServiceProvider.Returns(characterProvider);
            inventory.FreeSlots.Returns(1);
            inventory.Contains(1).Returns(true);
            statistics.GetSkillLevel(StatisticsConstants.Fishing).Returns(99);
            itemService.FindItemDefinitionById(1).Returns(Substitute.For<IItemDefinition>());
            itemService.FindItemDefinitionById(1).Name.Returns("small fishing net");

            characterProvider.GetService(typeof(IEntityService)).Returns(entityService);
            characterProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
            characterProvider.GetService(typeof(ILootGenerator)).Returns(lootGenerator);
            characterProvider.GetService(typeof(IItemService)).Returns(itemService);
            scope.ServiceProvider.Returns(scopedProvider);
            scopeFactory.CreateScope().Returns(scope);
            scopedProvider.GetService(typeof(IItemService)).Returns(itemService);
            scopedProvider.GetService(typeof(ILootGenerator)).Returns(lootGenerator);
            lootGenerator.GenerateLoot<IFishingLoot>(Arg.Any<CharacterLootParams>())
                .Returns(Array.Empty<LootResult<IFishingLoot>>());

            fixture.IsLive = true;
            fixture.CurrentCreature = originalSpot;
            entityService.TryResolve<ICreature>(handle, out Arg.Any<ICreature>()).Returns(callInfo =>
            {
                if (!fixture.IsLive)
                {
                    callInfo[1] = null;
                    return false;
                }

                callInfo[1] = fixture.CurrentCreature;
                return true;
            });

            var table = Substitute.For<IFishingSpotTable>();
            table.MinimumLevel.Returns(1);
            table.BaseCatchChance.Returns(1.0);
            table.ExhaustChance.Returns(1.0);
            table.RespawnTime.Returns(0.0);
            table.BaitId.Returns(0);
            table.RequiredTool.Returns(new FishingToolDto { ItemId = 1, FishAnimationId = 1, CastAnimationId = 0 });

            fixture.Table = table;
            fixture.FishingSkillService = new FishingSkillService(characterProvider, taskService, itemBuilder);
            character.QueueTask(Arg.Do<ITaskItem>(task => fixture.FishingTask = task as FishingTask))
                .Returns(Substitute.For<IRsTaskHandle>());
            taskService.Schedule(Arg.Do<ITaskItem>(task => fixture.RespawnTask = task));
            return fixture;
        }

        private sealed class FishingFixture
        {
            public required ICharacter Character { get; init; }
            public required IEntityService EntityService { get; init; }
            public required IFishingSkillService FishingSkillService { get; set; }
            public required EntityHandle<ICreature> FishingSpotHandle { get; init; }
            public required INpc OriginalSpot { get; init; }
            public required INpc TimerResolvedSpot { get; init; }
            public required INpcAppearance OriginalAppearance { get; init; }
            public required INpcAppearance TimerResolvedAppearance { get; init; }
            public IFishingSpotTable Table { get; set; } = null!;
            public bool IsLive { get; set; }
            public ICreature CurrentCreature { get; set; } = null!;
            public FishingTask? FishingTask { get; set; }
            public ITaskItem? RespawnTask { get; set; }
        }
    }
}
