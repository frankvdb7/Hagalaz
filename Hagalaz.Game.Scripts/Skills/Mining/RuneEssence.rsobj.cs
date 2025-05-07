using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Skills.Runecrafting;

namespace Hagalaz.Game.Scripts.Skills.Mining
{
    /// <summary>
    /// </summary>
    [GameObjectScriptMetaData([2491])]
    public class RuneEssence : GameObjectScript
    {
        /// <summary>
        ///     The message displayed when examining the rock.
        /// </summary>
        public const string Examine = "This rock contains rune essence.";

        /// <summary>
        ///     Experience received if logs were cut successfully.
        /// </summary>
        private const double _expAmount = 5.0;

        private readonly IMiningService _miningService;
        private readonly IItemBuilder _itemBuilder;

        public RuneEssence(IMiningService miningService, IItemBuilder itemBuilder)
        {
            _miningService = miningService;
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                var ore = _itemBuilder.Create()
                    .WithId(clicker.Statistics.GetSkillLevel(StatisticsConstants.Mining) >= 30
                        ? RunecraftingConstants.PureEssence
                        : RunecraftingConstants.RuneEssence)
                    .Build();
                const double mineChance = 0.75;
                clicker.QueueTask(() => StartRuneEssenceMining(clicker, Owner, ore, mineChance, _expAmount));
            }
            else if (clickType == GameObjectClickType.Option6Click)
            {
                clicker.SendChatMessage(Examine);
            }
        }

        /// <summary>
        ///     Starts the rune essence mining.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="rocks">The rocks.</param>
        /// <param name="ore">The ore.</param>
        /// <param name="mineChance">The mine chance.</param>
        /// <param name="expReceived">The exp recieved.</param>
        public async Task StartRuneEssenceMining(ICharacter character, IGameObject rocks, IItem ore, double mineChance, double expReceived)
        {
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
                character.SendChatMessage(MiningConstants.NoInventorySpace);
                return;
            }

            async ValueTask<bool> Callback()
            {
                await Task.CompletedTask;
                if (!character.Inventory.Add(ore))
                {
                    return false;
                }

                character.SendChatMessage(MiningConstants.OreReceived);
                character.Statistics.AddExperience(StatisticsConstants.Mining, expReceived);

                // No more space left to keep cutting.
                if (character.Inventory.FreeSlots >= 1)
                {
                    return false;
                }

                character.SendChatMessage(MiningConstants.NoInventorySpace);
                character.QueueAnimation(Animation.Create(-1));
                return true;
            }

            // queue the mining task.
            character.QueueTask(new MiningTask(character, Callback, mineChance, pickaxeData, rocks));
            character.QueueAnimation(Animation.Create(pickaxeData.AnimationId));
            character.SendChatMessage(MiningConstants.SwingPickaxe);
        }
    }
}