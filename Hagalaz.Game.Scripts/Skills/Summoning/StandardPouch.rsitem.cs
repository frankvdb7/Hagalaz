using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Summoning
{
    /// <summary>
    ///     Represents a pouch item script.
    /// </summary>
    public class StandardPouch : ItemScript
    {
        private readonly ISummoningSkillService _skillService;

        public StandardPouch(ISummoningSkillService skillService) => _skillService = skillService;

        /// <summary>
        ///     Happens when specific item is clicked in specific character's inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item which was clicked in character's inventory.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType != ComponentClickType.Option7Click)
            {
                base.ItemClickedInInventory(clickType, item, character);
                return;
            }

            character.QueueTask(async () =>
            {
                character.Interrupt(this);
                await _skillService.SummonFamiliar(character, item);
            });
        }
    }
}