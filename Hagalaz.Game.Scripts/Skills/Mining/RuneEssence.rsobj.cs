using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
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
                const double mineChance = 0.75;
                clicker.QueueTask(() => StartRuneEssenceMiningAsync(clicker, Owner, mineChance, _expAmount));
            }
            else if (clickType == GameObjectClickType.Option6Click)
            {
                clicker.SendChatMessage(Examine);
            }
        }

        private async Task StartRuneEssenceMiningAsync(
            ICharacter character,
            IGameObject rocks,
            double mineChance,
            double expReceived)
        {
            var pickaxes = await _miningService.FindAllPickaxes();
            BeginRuneEssenceMining(character, rocks, pickaxes, mineChance, expReceived);
        }

        private void BeginRuneEssenceMining(
            ICharacter character,
            IGameObject rocks,
            IReadOnlyList<PickaxeDto> pickaxes,
            double mineChance,
            double expReceived)
        {
            var ore = _itemBuilder.Create()
                .WithId(character.Statistics.GetSkillLevel(StatisticsConstants.Mining) >= 30
                    ? RunecraftingConstants.PureEssence
                    : RunecraftingConstants.RuneEssence)
                .Build();

            // check if character has usable pickaxe.
            var pickaxeData = Mining.FindPickaxe(character, pickaxes);
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

            bool Callback()
            {
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
