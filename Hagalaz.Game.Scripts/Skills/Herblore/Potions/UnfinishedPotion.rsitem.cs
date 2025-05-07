using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Potions
{
    /// <summary>
    /// </summary>
    public class UnfinishedPotion : ItemScript
    {
        private readonly IPotionSkillService _potionSkillService;

        public UnfinishedPotion(IPotionSkillService potionSkillService) => _potionSkillService = potionSkillService;

        /// <summary>
        ///     Happens when specific item is clicked in specific character's inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item which was clicked in character's inventory.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.Option7Click)
            {
                if (_potionSkillService.EmptyPotion(character, item))
                {
                    return;
                }
            }

            base.ItemClickedInInventory(clickType, item, character);
        }
    }
}