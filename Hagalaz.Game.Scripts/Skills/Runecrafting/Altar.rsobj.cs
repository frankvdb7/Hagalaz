using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Runecrafting
{
    /// <summary>
    /// </summary>
    public class Altar : GameObjectScript
    {
        private const int PureEssenceRequiredLevel = 27;

        private readonly IRunecraftingService _runecraftingService;
        private readonly IItemBuilder _itemBuilder;

        public Altar(IRunecraftingService runecraftingService, IItemBuilder itemBuilder)
        {
            _runecraftingService = runecraftingService;
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Happens when character click's this object and then walks to it
        ///     and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacterClick is overrided
        ///     than this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                clicker.QueueTask(cancellationToken => CraftRunes(clicker, cancellationToken));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Crafts the runes.
        /// </summary>
        /// <param name="character">The character.</param>
        public async Task CraftRunes(ICharacter character, System.Threading.CancellationToken cancellationToken = default)
        {
            var altarId = Owner.Id;
            var definition = await _runecraftingService.FindAltarById(altarId);
            cancellationToken.ThrowIfCancellationRequested();
            if (definition == null)
            {
                return;
            }

            var hasRuneEssence = character.Inventory.Items.Contains(RunecraftingConstants.RuneEssence);
            var hasPureEssence = character.Inventory.Items.Contains(RunecraftingConstants.PureEssence);
            if (!hasRuneEssence && !hasPureEssence)
            {
                character.SendChatMessage("You do not have any essence to craft runes!");
                return;
            }

            if (character.Statistics.GetSkillLevel(StatisticsConstants.Runecrafting) < definition.RequiredLevel)
            {
                character.SendChatMessage("You need a Runecrafting level of atleast " + definition.RequiredLevel + " in order to craft these runes.");
                return;
            }

            // Altars at level 27+ accept only pure essence; basic altars prefer ordinary essence so pure essence is kept.
            var requiresPureEssence = definition.RequiredLevel >= PureEssenceRequiredLevel;
            if (requiresPureEssence && !hasPureEssence)
            {
                character.SendChatMessage("You need to use pure essence in order to craft these runes.");
                return;
            }

            var usingPureEssence = requiresPureEssence || !hasRuneEssence;

            var removed = character.Inventory.RemoveItem(_itemBuilder.Create()
                .WithId(usingPureEssence ? RunecraftingConstants.PureEssence : RunecraftingConstants.RuneEssence)
                .WithCount(character.Inventory.Items.GetCountById(usingPureEssence ? RunecraftingConstants.PureEssence : RunecraftingConstants.RuneEssence))
                .Build());
            if (removed <= 0)
            {
                return;
            }

            character.QueueAnimation(Animation.Create(RunecraftingConstants.AnimationId));
            character.QueueGraphic(Graphic.Create(186));

            var runes = _itemBuilder.Create().WithId(definition.RuneId).WithCount(removed * Runecrafting.GetMultiplier(character, definition)).Build();
            character.Inventory.AddItem(runes);
            character.Statistics.AddExperience(StatisticsConstants.Runecrafting, definition.Experience * removed);
            character.SendChatMessage("You bind the temple's power into " + runes.Name + "s.");
        }
    }
}
