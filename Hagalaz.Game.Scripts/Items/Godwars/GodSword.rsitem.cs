using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Godwars
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData([11694, 11696, 11698, 11700])]
    public class GodSword : ItemScript
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
                var slot = character.Inventory.GetInstanceSlot(item);
                if (slot == -1)
                {
                    return;
                }

                if (character.Inventory.FreeSlots < 2)
                {
                    character.SendChatMessage("You do not have enough inventory space in order to dismantle this item!");
                    return;
                }

                if (character.Inventory.Remove(item, slot) > 0)
                {
                    character.Inventory.Add(new Item(11690, 1));
                    var hiltId = -1;
                    if (item.Id == 11694)
                    {
                        hiltId = 11702;
                    }
                    else if (item.Id == 11696)
                    {
                        hiltId = 11704;
                    }
                    else if (item.Id == 11698)
                    {
                        hiltId = 11706;
                    }
                    else if (item.Id == 11700)
                    {
                        hiltId = 11708;
                    }

                    character.Inventory.Add(new Item(hiltId, 1));
                }

                return;
            }

            base.ItemClickedInInventory(clickType, item, character);
        }
    }
}