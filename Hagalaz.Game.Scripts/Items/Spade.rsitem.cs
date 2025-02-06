using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData([952])]
    public class Spade : ItemScript
    {
        /*public override void ItemClickedInInventory(Hagalaz.Game.Model.RSInterfaces.ComponentClickType clickType, Hagalaz.Game.Model.RSItems.Item item, Hagalaz.Game.Model.Characters.Character character)
        {
            if (clickType == Hagalaz.Game.Model.RSInterfaces.ComponentClickType.LeftClick)
            {

                character.Interrupt(true);
                character.QueueAnimation(Animation.Create(830));
                new ItemClickedEvent(character, item, clickType, false).Send();
            }
            else
            {
                base.ItemClickedInInventory(clickType, item, character);
            }
        }*/
    }
}