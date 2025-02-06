using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Godwars
{
    /// <summary>
    ///     Contains the item script for decharged zaros armour.
    /// </summary>
    [ItemScriptMetaData([
        20137, 20141, 20145, // torva
        20149, 20153, 20157, // pernix
        20161, 20165, 20169 // virtus
    ])]
    public class ZarosDechargedArmour : ItemScript
    {
        /// <summary>
        ///     Happens when specific item is clicked in specific character's inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item which was clicked in character's inventory.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.Option3Click)
            {
                var charges = item.ExtraData[0];
                if (charges == -1)
                {
                    character.SendChatMessage(item.Name + " has all its charges remaining.");
                }
                else
                {
                    character.SendChatMessage(item.Name + " has " + charges + " charges remaining.");
                }
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
                var charges = item.ExtraData[0];
                if (charges == -1)
                {
                    character.SendChatMessage(item.Name + " has all its charges remaining.");
                }
                else
                {
                    character.SendChatMessage(item.Name + " has " + charges + " charges remaining.");
                }
            }
            else
            {
                base.ItemClickedInEquipment(clickType, item, character);
            }
        }
    }
}