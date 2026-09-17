using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
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
    }
}
