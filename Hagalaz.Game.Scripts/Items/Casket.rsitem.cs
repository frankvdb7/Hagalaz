using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData([405])]
    public class Casket : ItemScript
    {
        /// <summary>
        ///     The loot manager
        /// </summary>
        private readonly ILootService _lootService;

        /// <summary>
        /// 
        /// </summary>
        public Casket(ILootService lootService) => _lootService = lootService;

        /// <summary>
        ///     Happens when specific item is clicked in specific character's inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item which was clicked in character's inventory.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.LeftClick)
            {
                character.SendChatMessage("You tried to open the casket...");
                character.QueueTask(async () =>
                {
                    var slot = character.Inventory.GetInstanceSlot(item);
                    if (slot == -1)
                    {
                        return;
                    }

                    character.Inventory.Remove(item, slot);
                    var table = await _lootService.FindItemLootTable(1); // 1 == casket loot table
                    if (table == null)
                    {
                        return;
                    }

                    character.Inventory.TryAddLoot(character, table, out var items);
                    character.SendChatMessage(items.Any() ? "and found some glorious loot!" : "and found nothing...");
                });
            }
            else
            {
                base.ItemClickedInInventory(clickType, item, character);
            }
        }
    }
}