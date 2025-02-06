using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Rings
{
    [ItemScriptMetaData([2552, 2554, 2556, 2558, 2560, 2562, 2564, 2566])]
    public class RingOfDueling : ItemScript
    {
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
                var dialogue = character.ServiceProvider.GetRequiredService<RingOfDuelingDialogue>();
                character.Widgets.OpenDialogue(dialogue, true, item);
            }
            else
            {
                base.ItemClickedInInventory(clickType, item, character);
            }
        }

        /// <summary>
        ///     Happens when specific item is clicked in specific character's equipment.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item in character's equipment.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInEquipment(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.Option2Click)
            {
                Jewelry.Jewelry.TeleportRingOfDueling(character, item, true, Jewelry.Jewelry.RingOfDuelingTeleports[0]);
            }
            else if (clickType == ComponentClickType.Option3Click)
            {
                Jewelry.Jewelry.TeleportRingOfDueling(character, item, true, Jewelry.Jewelry.RingOfDuelingTeleports[1]);
            }
            else if (clickType == ComponentClickType.Option4Click)
            {
                Jewelry.Jewelry.TeleportRingOfDueling(character, item, true, Jewelry.Jewelry.RingOfDuelingTeleports[2]);
            }
            else if (clickType == ComponentClickType.Option5Click)
            {
                Jewelry.Jewelry.TeleportRingOfDueling(character, item, true, Jewelry.Jewelry.RingOfDuelingTeleports[3]);
            }
            else
            {
                base.ItemClickedInEquipment(clickType, item, character);
            }
        }
    }
}