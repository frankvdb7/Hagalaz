using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Herblore.Herbs
{
    /// <summary>
    ///     Contains grimy vial script.
    /// </summary>
    public class GrimyHerbItemScript : ItemScript
    {
        private readonly IHerbloreSkillService _herblore;

        public GrimyHerbItemScript(IHerbloreSkillService herblore) => _herblore = herblore;

        /// <summary>
        ///     Happens when character clicks specific item in inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item which was clicked in character's inventory.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType != ComponentClickType.LeftClick)
            {
                base.ItemClickedInInventory(clickType, item, character);
                return;
            }

            character.Interrupt(this);
            character.QueueTask(() => _herblore.TryCleanHerb(character, item));
        }
    }
}