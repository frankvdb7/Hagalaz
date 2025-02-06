using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Amulets
{
    [ItemScriptMetaData([1712, 1710, 1708, 1706])]
    public class AmuletOfGlory : ItemScript
    {
        /// <summary>
        ///     Happens when specific item is clicked in specific character's equipment.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item in character's equipment.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInEquipment(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.Option2Click) // edgeville
            {
                Jewelry.Jewelry.TeleportAmuletOfGlory(character, item, true, Jewelry.Jewelry.GloryTeleports[0]);
            }
            else if (clickType == ComponentClickType.Option3Click) // karamja
            {
                Jewelry.Jewelry.TeleportAmuletOfGlory(character, item, true, Jewelry.Jewelry.GloryTeleports[1]);
            }
            else if (clickType == ComponentClickType.Option4Click) // draynor village
            {
                Jewelry.Jewelry.TeleportAmuletOfGlory(character, item, true, Jewelry.Jewelry.GloryTeleports[2]);
            }
            else if (clickType == ComponentClickType.Option5Click) // al-kharid
            {
                Jewelry.Jewelry.TeleportAmuletOfGlory(character, item, true, Jewelry.Jewelry.GloryTeleports[3]);
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
                var dialogue = character.ServiceProvider.GetRequiredService<AmuletOfGloryDialogue>();
                character.Widgets.OpenDialogue(dialogue, true, item);
            }
            else
            {
                base.ItemClickedInInventory(clickType, item, character);
            }
        }
    }
}