using System.Threading.Tasks;
using System;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
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
                clicker.QueueTask(() => StartMining(clicker, Owner));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }


        /// <summary>
        ///     Starts the mining.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="rocks">The rocks.</param>
        /// <returns></returns>
        public async Task StartMining(ICharacter character, IGameObject rocks)
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

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Mining) < ore.RequiredLevel)
            {
                character.SendChatMessage("You must have a mining level of " + ore.RequiredLevel + " or higher to mine this rock.");
                return;
            }

            await BeginMining(character, rocks, ore.ExhaustChance, ore.BaseHarvestChance, ore.Experience, rock.ExhaustRockId, ore.RespawnTime);
        }

        /// <summary>
        ///     Starts the mining.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="rocks">The rocks.</param>
        /// <param name="exhaustChance">The exhaust chance.</param>
        /// <param name="baseHarvestChance">The mine chance.</param>
        /// <param name="expReceived">The exp received.</param>
        /// <param name="exhaustedRockId">The exhausted rocks.</param>
        /// <param name="respawnTime">The respawn time in minutes.</param>
        private async Task BeginMining(ICharacter character, IGameObject rocks, double exhaustChance, double baseHarvestChance, double expReceived, int exhaustedRockId, double respawnTime)
        {
            if (rocks.IsDestroyed || rocks.IsDisabled)
            {
                character.SendChatMessage(MiningConstants.RockAlreadyMined);
                return;
            }

            // check if character has usable pickaxe.
            var pickaxeData = Mining.FindPickaxe(character, await _miningService.FindAllPickaxes());
            if (pickaxeData == null)
            {
                character.SendChatMessage(MiningConstants.NoPickaxeFound);
                return;
            }

            // check if there is enough space in the character's inventory.
            if (character.Inventory.FreeSlots < 1)
            {
                character.SendChatMessage(GameStrings.InventoryFull);
                return;
            }

            var miningBasedChance = Math.Log10(Math.Log10(character.Statistics.GetSkillLevel(StatisticsConstants.Mining))) * 0.075;
            var harvestChance = baseHarvestChance + pickaxeData.BaseHarvestChance;
            if (miningBasedChance > 0.0)
            {
                harvestChance += miningBasedChance;
            }

            async ValueTask<bool> Callback()
            {
                var table = await _miningService.FindRockLootById(Owner.Id);
                if (table == null)
                {
                    return true;
                }

                character.Inventory.TryAddLoot(character, table, out _);
                character.SendChatMessage(MiningConstants.OreReceived);
                character.Statistics.AddExperience(StatisticsConstants.Mining, expReceived);

                // Calculate the chance of the rock exhaust
                var randomVal = RandomStatic.Generator.NextDouble();
                if (randomVal <= exhaustChance)
                {
                    character.QueueAnimation(Animation.Reset);

                    var goBuilder = character.ServiceProvider.GetRequiredService<IGameObjectBuilder>();
                    // replace it with a exhausted rock.
                    if (exhaustedRockId > 0)
                    {
                        var exhaustedRock = goBuilder.Create()
                            .WithId(exhaustedRockId)
                            .WithLocation(rocks.Location)
                            .WithRotation(rocks.Rotation)
                            .WithShape(rocks.ShapeType)
                            .Build();
                        rocks.Region.Add(exhaustedRock);
                    }
                    else // delete the rocks
                    {
                        rocks.Region.Remove(rocks);
                    }

                    var characterCount = await _characterStore.CountAsync();
                    var respawnTick = (int)(respawnTime * (1.0 + characterCount * -0.00025) * 100.0);

                    _taskService.Schedule(new RsTask(() => rocks.Region.Add(rocks), respawnTick));
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