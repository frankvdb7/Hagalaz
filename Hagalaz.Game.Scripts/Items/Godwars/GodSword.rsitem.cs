using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Godwars
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData([11694, 11696, 11698, 11700])]
    public class GodSword : ItemScript
    {
        private readonly IItemBuilder _itemBuilder;

        public GodSword(IItemBuilder itemBuilder)
        {
            _itemBuilder = itemBuilder;
        }

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

                if (character.Inventory.Remove(item, slot) <= 0)
                {
                    return;
                }

                character.Inventory.Add(_itemBuilder.Create().WithId(11690).Build());
                var hiltId = item.Id switch
                {
                    11694 => 11702,
                    11696 => 11704,
                    11698 => 11706,
                    11700 => 11708,
                    _ => -1
                };

                character.Inventory.Add(_itemBuilder.Create().WithId(hiltId).Build());

                return;
            }

            base.ItemClickedInInventory(clickType, item, character);
        }
    }
}