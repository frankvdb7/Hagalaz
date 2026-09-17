using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Skills.Mining;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Mining
{
    [TestClass]
    public sealed class MiningIdentityTests
    {
        [TestMethod]
        public async Task MiningRocks_WhenTargetIsReplacedDuringSetup_DoesNotQueueMiningTask()
        {
            var miningService = Substitute.For<IMiningService>();
            var taskService = Substitute.For<IRsTaskService>();
            var characterStore = Substitute.For<ICharacterStore>();
            var entityService = Substitute.For<IEntityService>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
            var character = Substitute.For<ICharacter>();
            character.ServiceProvider.Returns(serviceProvider);

            var rocks = Substitute.For<IGameObject>();
            var handle = new EntityHandle<IGameObject>(11, 1);
            rocks.Handle.Returns(handle);
            rocks.Id.Returns(1);
            var isLive = true;
            entityService.TryResolve<IGameObject>(handle, out Arg.Any<IGameObject>()).Returns(callInfo =>
            {
                if (!isLive)
                {
                    callInfo[1] = null;
                    return false;
                }

                callInfo[1] = rocks;
                return true;
            });

            var rockLookup = new TaskCompletionSource<RockDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
            miningService.FindRockById(rocks.Id).Returns(rockLookup.Task);
            miningService.FindOreByRockId(rocks.Id).Returns(Task.FromResult<OreDto?>(new OreDto
            {
                ItemId = 1,
                RequiredLevel = 1,
                Experience = 1,
                RespawnTime = 1,
                BaseHarvestChance = 0.1,
                ExhaustChance = 0.1,
            }));
            miningService.FindAllPickaxes().Returns(Task.FromResult<IReadOnlyList<PickaxeDto>>([]));
            miningService.FindRockLootById(rocks.Id).Returns(Task.FromResult<ILootTable?>(null));

            Func<CancellationToken, Task>? operation = null;
            character.QueueTask(Arg.Do<Func<CancellationToken, Task>>(value => operation = value))
                .Returns(Substitute.For<IRsTaskHandle>());

            var script = new MiningRocks(miningService, taskService, characterStore);
            script.Initialize(rocks);
            script.OnCharacterClickPerform(character, GameObjectClickType.Option1Click);
            Assert.IsNotNull(operation);

            var startup = operation!(CancellationToken.None);
            isLive = false;
            rockLookup.SetResult(new RockDto { RockId = rocks.Id, ExhaustRockId = 0, OreId = 1 });
            await startup;

            character.DidNotReceive().QueueTask(Arg.Any<MiningTask>());
        }

        [TestMethod]
        public async Task RuneEssence_WhenTargetIsReplacedAfterSetup_DoesNotQueueMiningTask()
        {
            var miningService = Substitute.For<IMiningService>();
            var itemBuilder = Substitute.For<IItemBuilder>();
            var entityService = Substitute.For<IEntityService>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
            var character = Substitute.For<ICharacter>();
            character.ServiceProvider.Returns(serviceProvider);

            var rocks = Substitute.For<IGameObject>();
            var handle = new EntityHandle<IGameObject>(12, 1);
            rocks.Handle.Returns(handle);
            var isLive = true;
            entityService.TryResolve<IGameObject>(handle, out Arg.Any<IGameObject>()).Returns(callInfo =>
            {
                if (!isLive)
                {
                    callInfo[1] = null;
                    return false;
                }

                callInfo[1] = rocks;
                return true;
            });

            var pickaxesLookup = new TaskCompletionSource<IReadOnlyList<PickaxeDto>>(TaskCreationOptions.RunContinuationsAsynchronously);
            miningService.FindAllPickaxes().Returns(pickaxesLookup.Task);

            Func<CancellationToken, Task>? operation = null;
            character.QueueTask(Arg.Do<Func<CancellationToken, Task>>(value => operation = value))
                .Returns(Substitute.For<IRsTaskHandle>());

            var script = new RuneEssence(miningService, itemBuilder);
            script.Initialize(rocks);
            script.OnCharacterClickPerform(character, GameObjectClickType.Option1Click);
            Assert.IsNotNull(operation);

            var startup = operation!(CancellationToken.None);
            isLive = false;
            pickaxesLookup.SetResult([]);
            await startup;

            character.DidNotReceive().QueueTask(Arg.Any<MiningTask>());
        }

        [TestMethod]
        public async Task Mining_WhenTargetBecomesStaleBeforeRespawn_DoesNotReaddIt()
        {
            var fixture = CreateMiningFixture();
            await fixture.StartOperation(CancellationToken.None);

            Assert.IsNotNull(fixture.QueuedMiningTask);
            fixture.QueuedMiningTask!.Tick();
            Assert.IsNotNull(fixture.ScheduledRespawn);

            fixture.IsLive = false;
            fixture.ScheduledRespawn!.Tick();

            fixture.MapRegionService.DidNotReceive().AddGameObject(Arg.Any<IGameObject>());
        }

        [TestMethod]
        public async Task Mining_WhenOriginalStaticStillResolvesDisabled_RespawnsExactResolvedTarget()
        {
            var fixture = CreateMiningFixture();
            await fixture.StartOperation(CancellationToken.None);

            Assert.IsNotNull(fixture.QueuedMiningTask);
            fixture.QueuedMiningTask!.Tick();
            Assert.IsNotNull(fixture.ScheduledRespawn);

            fixture.ResolvedRock.IsDisabled.Returns(true);
            fixture.ScheduledRespawn!.Tick();

            fixture.MapRegionService.Received(1).AddGameObject(fixture.ResolvedRock);
            fixture.MapRegionService.DidNotReceive().AddGameObject(fixture.OriginalRock);
        }

        private static MiningFixture CreateMiningFixture()
        {
            var miningService = Substitute.For<IMiningService>();
            var taskService = Substitute.For<IRsTaskService>();
            var characterStore = Substitute.For<ICharacterStore>();
            var entityService = Substitute.For<IEntityService>();
            var mapRegionService = Substitute.For<IMapRegionService>();
            var lootGenerator = Substitute.For<ILootGenerator>();
            var itemBuilder = Substitute.For<IItemBuilder>();
            var gameObjectBuilder = Substitute.For<IGameObjectBuilder>();
            var groundItemBuilder = Substitute.For<IGroundItemBuilder>();
            var serviceProvider = Substitute.For<IServiceProvider>();
            var character = Substitute.For<ICharacter>();
            var inventory = Substitute.For<IInventoryContainer>();
            var equipment = Substitute.For<IEquipmentContainer>();
            var statistics = Substitute.For<ICharacterStatistics>();
            var originalRock = Substitute.For<IGameObject>();
            var resolvedRock = Substitute.For<IGameObject>();
            var handle = new EntityHandle<IGameObject>(13, 1);
            var fixture = new MiningFixture
            {
                EntityService = entityService,
                MapRegionService = mapRegionService,
                OriginalRock = originalRock,
                ResolvedRock = resolvedRock,
            };

            originalRock.Handle.Returns(handle);
            originalRock.Id.Returns(1);
            resolvedRock.Handle.Returns(handle);
            resolvedRock.Id.Returns(1);
            resolvedRock.IsDisabled.Returns(false);
            fixture.IsLive = true;
            entityService.TryResolve<IGameObject>(handle, out Arg.Any<IGameObject>()).Returns(callInfo =>
            {
                if (!fixture.IsLive)
                {
                    callInfo[1] = null;
                    return false;
                }

                callInfo[1] = resolvedRock;
                return true;
            });

            var pickaxe = new PickaxeDto
            {
                Type = PickaxeType.Bronze,
                ItemId = 1,
                AnimationId = 1,
                RequiredLevel = 1,
                BaseHarvestChance = 0.0,
            };
            var ore = new OreDto
            {
                ItemId = 1,
                RequiredLevel = 1,
                Experience = 1,
                RespawnTime = 0,
                BaseHarvestChance = 1.0,
                ExhaustChance = 1.0,
            };
            miningService.FindRockById(1).Returns(Task.FromResult<RockDto?>(new RockDto
            {
                RockId = 1,
                ExhaustRockId = 0,
                OreId = 1,
            }));
            miningService.FindOreByRockId(1).Returns(Task.FromResult<OreDto?>(ore));
            miningService.FindAllPickaxes().Returns(Task.FromResult<IReadOnlyList<PickaxeDto>>([pickaxe]));
            miningService.FindRockLootById(1).Returns(Task.FromResult<ILootTable?>(Substitute.For<ILootTable>()));
            characterStore.CountAsync().Returns(ValueTask.FromResult(0));

            inventory.FreeSlots.Returns(1);
            equipment.GetById(pickaxe.ItemId).Returns(Substitute.For<IItem>());
            statistics.GetSkillLevel(StatisticsConstants.Mining).Returns(99);
            character.Inventory.Returns(inventory);
            character.Equipment.Returns(equipment);
            character.Statistics.Returns(statistics);
            character.ServiceProvider.Returns(serviceProvider);
            lootGenerator.GenerateLoot<ILootItem>(Arg.Any<CharacterLootParams>())
                .Returns(Array.Empty<LootResult<ILootItem>>());

            serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
            serviceProvider.GetService(typeof(IMapRegionService)).Returns(mapRegionService);
            serviceProvider.GetService(typeof(ILootGenerator)).Returns(lootGenerator);
            serviceProvider.GetService(typeof(IItemBuilder)).Returns(itemBuilder);
            serviceProvider.GetService(typeof(IGameObjectBuilder)).Returns(gameObjectBuilder);
            serviceProvider.GetService(typeof(IGroundItemBuilder)).Returns(groundItemBuilder);

            Func<CancellationToken, Task>? operation = null;
            character.QueueTask(Arg.Do<Func<CancellationToken, Task>>(value => operation = value))
                .Returns(Substitute.For<IRsTaskHandle>());
            character.QueueTask(Arg.Do<ITaskItem>(task => fixture.QueuedMiningTask = task as MiningTask))
                .Returns(Substitute.For<IRsTaskHandle>());
            taskService.Schedule(Arg.Do<ITaskItem>(task => fixture.ScheduledRespawn = task));

            var script = new MiningRocks(miningService, taskService, characterStore);
            script.Initialize(originalRock);
            script.OnCharacterClickPerform(character, GameObjectClickType.Option1Click);
            fixture.StartOperation = operation is null
                ? throw new InvalidOperationException("The mining operation was not queued.")
                : operation;

            return fixture;
        }

        private sealed class MiningFixture
        {
            public required IEntityService EntityService { get; init; }
            public required IMapRegionService MapRegionService { get; init; }
            public required IGameObject OriginalRock { get; init; }
            public required IGameObject ResolvedRock { get; init; }
            public bool IsLive { get; set; }
            public Func<CancellationToken, Task> StartOperation { get; set; } = null!;
            public MiningTask? QueuedMiningTask { get; set; }
            public ITaskItem? ScheduledRespawn { get; set; }
        }
    }
}
