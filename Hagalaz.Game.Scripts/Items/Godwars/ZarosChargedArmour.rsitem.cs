using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Godwars
{
    /// <summary>
    ///     Contains the item script for charged zaros armour.
    /// </summary>
    [ItemScriptMetaData([
        20135, 20139, 20143, // torva
        20147, 20151, 20155, // pernix
        20159, 20163, 20167 // virtus
    ])]
    public class ZarosChargedArmour : ItemScript
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
                character.SendChatMessage(item.Name + " has all its charges remaining.");
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
                character.SendChatMessage(item.Name + " has all its charges remaining.");
            }
            else
            {
                base.ItemClickedInEquipment(clickType, item, character);
            }
        }
    }
}