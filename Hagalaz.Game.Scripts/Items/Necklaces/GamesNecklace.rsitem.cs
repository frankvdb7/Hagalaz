using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Necklaces
{
    [ItemScriptMetaData([3853, 3855, 3857, 3859, 3861, 3863, 3865, 3867])]
    public class GamesNecklace : ItemScript
    {
        /// <summary>
        ///     Happens when specific item is clicked in specific character's equipment.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item in character's equipment.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInEquipment(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.Option4Click) // gamer's grotto
            {
                Jewelry.Jewelry.TeleportGamesNecklace(character, item, true, Jewelry.Jewelry.GameNecklaceTeleports[0]);
            }
            else if (clickType == ComponentClickType.Option5Click) // corporeal beast
            {
                Jewelry.Jewelry.TeleportGamesNecklace(character, item, true, Jewelry.Jewelry.GameNecklaceTeleports[1]);
            }
            else
            {
                base.ItemClickedInEquipment(clickType, item, character);
            }
        }

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
                var dialogue = character.ServiceProvider.GetRequiredService<GamesNecklaceDialogue>();
                character.Widgets.OpenDialogue(dialogue, true, item);
            }
            else
            {
                base.ItemClickedInInventory(clickType, item, character);
            }
        }
    }
}