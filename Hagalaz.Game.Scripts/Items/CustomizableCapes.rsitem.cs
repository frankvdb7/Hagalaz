using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Scripts.Widgets.CapeDesign;

namespace Hagalaz.Game.Scripts.Items
{
    [ItemScriptMetaData([20767, 20769, 20771])]
    public class CustomizableCapes : ItemScript
    {
        /// <summary>
        ///     Happens when specific item is clicked in specific character's equipment.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item in character's equipment.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInEquipment(ComponentClickType clickType, IItem item, ICharacter character)
        {
            // TODO - Different options.
            if (item.Id == 20767 && clickType == ComponentClickType.Option2Click || (item.Id == 20769 || item.Id == 20771) && clickType == ComponentClickType.Option5Click)
            {
                var customizeScript = character.ServiceProvider.GetRequiredService<CustomizeInterfaceScript>();
                customizeScript.ToCustomize = item;
                character.Widgets.OpenWidget(20, 0, customizeScript, true);
                return;
            }

            base.ItemClickedInEquipment(clickType, item, character);
        }

        /// <summary>
        ///     Happens when specific item is clicked in specific character's equipment.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item in character's equipment.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType != ComponentClickType.Option3Click)
            {
                base.ItemClickedInInventory(clickType, item, character);
                return;
            }
            var customizeScript = character.ServiceProvider.GetRequiredService<CustomizeInterfaceScript>();
            customizeScript.ToCustomize = item;
            character.Widgets.OpenWidget(20, 0, customizeScript, true);
        }
    }
}