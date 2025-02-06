using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Weapons
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData([4151, 15441, 15442, 15443, 15444])]
    public class AbyssalWhip : ItemScript
    {
        /// <summary>
        ///     Uses the item on an other item.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///     <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character)
        {
            var whipIsUsedItem = false;
            var colour = ColourMethods.GetColouringItem(used);
            if (colour == ColouringItems.None)
            {
                whipIsUsedItem = true;
                colour = ColourMethods.GetColouringItem(usedWith);
            }

            Item colouredItem = null;
            if (colour == ColouringItems.WhiteFish)
            {
                colouredItem = new Item(15443, 1);
            }
            else if (colour == ColouringItems.BlueDye)
            {
                colouredItem = new Item(15442, 1);
            }
            else if (colour == ColouringItems.YellowDye)
            {
                colouredItem = new Item(15441, 1);
            }
            else if (colour == ColouringItems.GreenDye)
            {
                colouredItem = new Item(15444, 1);
            }
            else if (colour == ColouringItems.CleaningCloth)
            {
                colouredItem = new Item(4151, 1);
            }
            else
            {
                return base.UseItemOnItem(used, usedWith, character);
            }


            var whipSlot = -1;
            var colourSlot = -1;
            if (whipIsUsedItem)
            {
                whipSlot = character.Inventory.GetInstanceSlot(used);
                colourSlot = character.Inventory.GetInstanceSlot(usedWith);
            }
            else
            {
                whipSlot = character.Inventory.GetInstanceSlot(usedWith);
                colourSlot = character.Inventory.GetInstanceSlot(used);
            }

            if (whipSlot == -1 || colourSlot == -1)
            {
                return false;
            }

            var removed = character.Inventory.Remove(whipIsUsedItem ? usedWith : used, colourSlot);
            if (removed > 0)
            {
                character.Inventory.Replace(whipSlot, colouredItem);
                return true;
            }

            return false;
        }
    }
}