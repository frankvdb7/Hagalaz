using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Mining
{
    /// <summary>
    ///     Represents a iron rock.
    /// </summary>
    public class MiningRocks : GameObjectScript
    {
        private readonly IMiningService _miningService;
        private readonly IRsTaskService _taskService;
        private readonly ICharacterStore _characterStore;

        public MiningRocks(IMiningService miningService, IRsTaskService taskService, ICharacterStore characterStore)
        {
            _miningService = miningService;
            _taskService = taskService;
            _characterStore = characterStore;
        }

        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize() {}

        /// <summary>
        ///     Happens on character click.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                var rocksHandle = Owner.Handle;
                clicker.QueueTask(cancellationToken => StartMiningAsync(clicker, rocksHandle, cancellationToken));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        private async Task StartMiningAsync(
            ICharacter character,
            EntityHandle<IGameObject> rocksHandle,
            System.Threading.CancellationToken cancellationToken)
        {
            var entityService = character.ServiceProvider.GetRequiredService<IEntityService>();
            if (!entityService.TryResolve(rocksHandle, out var rocks) || rocks is null || rocks.IsDisabled)
            {
                return;
            }

            var rocksId = rocks.Id;
            var interrupted = false;
            var interruptEvent = character.RegisterEventHandler<CreatureInterruptedEvent>(_ =>
            {
                interrupted = true;
                return false;
            });

            try
            {
                var rock = await _miningService.FindRockById(rocksId);
                if (rock is null)
                {
                    return;
                }

                var ore = await _miningService.FindOreByRockId(rocksId);
                if (ore is null)
                {
                    return;
                }

                var pickaxes = await _miningService.FindAllPickaxes();
                var lootTable = await _miningService.FindRockLootById(rocksId);
                var characterCount = await _characterStore.CountAsync();
                cancellationToken.ThrowIfCancellationRequested();

                if (!interrupted
                    && entityService.TryResolve(rocksHandle, out var currentRocks)
                    && currentRocks is not null
                    && !currentRocks.IsDisabled)
                {
                    StartMining(character, rocksHandle, ore, rock, pickaxes, lootTable, characterCount);
                }
            }
            finally
            {
                character.UnregisterEventHandler<CreatureInterruptedEvent>(interruptEvent);
            }
        }

        private void StartMining(
            ICharacter character,
            EntityHandle<IGameObject> rocksHandle,
            OreDto ore,
            RockDto rock,
            IReadOnlyList<PickaxeDto> pickaxes,
            ILootTable? lootTable,
            int characterCount)
        {
            var entityService = character.ServiceProvider.GetRequiredService<IEntityService>();
            if (!entityService.TryResolve(rocksHandle, out var rocks) || rocks is null || rocks.IsDisabled)
            {
                if (rocks?.IsDisabled == true)
                {
                    character.SendChatMessage(MiningConstants.RockAlreadyMined);
                }

                return;
            }

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Mining) < ore.RequiredLevel)
            {
                character.SendChatMessage("You must have a mining level of " + ore.RequiredLevel + " or higher to mine this rock.");
                return;
            }

            var pickaxeData = Mining.FindPickaxe(character, pickaxes);
            if (pickaxeData == null)
            {
                character.SendChatMessage(MiningConstants.NoPickaxeFound);
                return;
            }

            if (character.Inventory.FreeSlots < 1)
            {
                character.SendChatMessage(GameStrings.InventoryFull);
                return;
            }

            if (lootTable == null)
            {
                return;
            }

            var miningBasedChance = Math.Log10(Math.Log10(character.Statistics.GetSkillLevel(StatisticsConstants.Mining))) * 0.075;
            var harvestChance = ore.BaseHarvestChance + pickaxeData.BaseHarvestChance;
            if (miningBasedChance > 0.0)
            {
                harvestChance += miningBasedChance;
            }

            bool Callback(IGameObject target)
            {
                character.Inventory.TryAddLoot(character, lootTable, out _);
                character.SendChatMessage(MiningConstants.OreReceived);
                character.Statistics.AddExperience(StatisticsConstants.Mining, ore.Experience);

                // Calculate the chance of the rock exhaust
                var randomVal = RandomStatic.Generator.NextDouble();
                if (randomVal <= ore.ExhaustChance)
                {
                    character.QueueAnimation(Animation.Reset);

                    var goBuilder = character.ServiceProvider.GetRequiredService<IGameObjectBuilder>();
                    // replace it with a exhausted rock.
                    if (rock.ExhaustRockId > 0)
                    {
                        var exhaustedRock = goBuilder.Create()
                            .WithId(rock.ExhaustRockId)
                            .WithLocation(target.Location)
                            .WithRotation(target.Rotation)
                            .WithShape(target.ShapeType)
                            .Build();
                        character.ServiceProvider.GetRequiredService<IMapRegionService>().AddGameObject(exhaustedRock);
                    }
                    else // delete the rocks
                    {
                        character.ServiceProvider.GetRequiredService<IMapRegionService>().RemoveGameObject(target);
                    }

                    var respawnTick = (int)(ore.RespawnTime * (1.0 + characterCount * -0.00025) * 100.0);
                    var targetHandle = target.Handle;

                    _taskService.Schedule(new RsTask(() =>
                    {
                        if (entityService.TryResolve(targetHandle, out var respawnTarget)
                            && respawnTarget is not null
                            && respawnTarget.IsDisabled)
                        {
                            character.ServiceProvider.GetRequiredService<IMapRegionService>().AddGameObject(respawnTarget);
                        }
                    }, respawnTick));
                    return true;
                }

                // No more space left to keep mining.
                if (character.Inventory.FreeSlots >= 1)
                {
                    return false; // keep mining
                }

                character.QueueAnimation(Animation.Reset);
                character.SendChatMessage(MiningConstants.NoInventorySpace);
                return true; // stop mining
            }

            // queue the mining task.
            if (!entityService.TryResolve(rocksHandle, out var currentRocksBeforeQueue)
                || currentRocksBeforeQueue is null
                || currentRocksBeforeQueue.IsDisabled)
            {
                return;
            }

            character.QueueTask(new MiningTask(character, Callback, harvestChance, pickaxeData, rocksHandle));
            character.QueueAnimation(Animation.Create(pickaxeData.AnimationId));
            character.SendChatMessage(MiningConstants.SwingPickaxe);
        }
    }
}
