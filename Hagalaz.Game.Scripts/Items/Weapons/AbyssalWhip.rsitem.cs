using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Weapons
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData([4151, 15441, 15442, 15443, 15444])]
    public class AbyssalWhip : ItemScript
    {
        private readonly IItemBuilder _itemBuilder;

        public AbyssalWhip(IItemBuilder itemBuilder) => _itemBuilder = itemBuilder;

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

            IItem colouredItem;
            switch (colour)
            {
                case ColouringItems.WhiteFish: colouredItem = _itemBuilder.Create().WithId(15443).Build(); break;
                case ColouringItems.BlueDye: colouredItem = _itemBuilder.Create().WithId(15442).Build(); break;
                case ColouringItems.YellowDye: colouredItem = _itemBuilder.Create().WithId(15441).Build(); break;
                case ColouringItems.GreenDye: colouredItem = _itemBuilder.Create().WithId(15444).Build(); break;
                case ColouringItems.CleaningCloth: colouredItem = _itemBuilder.Create().WithId(4151).Build(); break;
                default: return base.UseItemOnItem(used, usedWith, character);
            }


            int whipSlot;
            int colourSlot;
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