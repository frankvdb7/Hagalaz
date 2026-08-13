using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Logic.Loot;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Resources;
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
                clicker.QueueTask(() => StartMiningAsync(clicker, Owner));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        private async Task StartMiningAsync(ICharacter character, IGameObject rocks)
        {
            var rock = await _miningService.FindRockById(rocks.Id);
            if (rock == null)
            {
                return;
            }

            var ore = await _miningService.FindOreByRockId(rocks.Id);
            if (ore == null)
            {
                return;
            }

            var pickaxes = await _miningService.FindAllPickaxes();
            var lootTable = await _miningService.FindRockLootById(rocks.Id);
            var characterCount = await _characterStore.CountAsync();
            StartMining(character, rocks, ore, rock, pickaxes, lootTable, characterCount);
        }

        private void StartMining(
            ICharacter character,
            IGameObject rocks,
            OreDto ore,
            RockDto rock,
            IReadOnlyList<PickaxeDto> pickaxes,
            ILootTable? lootTable,
            int characterCount)
        {
            if (rocks.IsDestroyed || rocks.IsDisabled)
            {
                character.SendChatMessage(MiningConstants.RockAlreadyMined);
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

            bool Callback()
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
                            .WithLocation(rocks.Location)
                            .WithRotation(rocks.Rotation)
                            .WithShape(rocks.ShapeType)
                            .Build();
                        character.ServiceProvider.GetRequiredService<IMapRegionService>()
                            .GetOrCreateMapRegion(rocks.Location.RegionId, rocks.Location.Dimension, false)
                            .Add(exhaustedRock);
                    }
                    else // delete the rocks
                    {
                        character.ServiceProvider.GetRequiredService<IMapRegionService>()
                            .GetOrCreateMapRegion(rocks.Location.RegionId, rocks.Location.Dimension, false)
                            .Remove(rocks);
                    }

                    var respawnTick = (int)(ore.RespawnTime * (1.0 + characterCount * -0.00025) * 100.0);

                    _taskService.Schedule(new RsTask(() =>
                        character.ServiceProvider.GetRequiredService<IMapRegionService>()
                            .GetOrCreateMapRegion(rocks.Location.RegionId, rocks.Location.Dimension, false)
                            .Add(rocks), respawnTick));
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
            character.QueueTask(new MiningTask(character, Callback, harvestChance, pickaxeData, rocks));
            character.QueueAnimation(Animation.Create(pickaxeData.AnimationId));
            character.SendChatMessage(MiningConstants.SwingPickaxe);
        }
    }
}
