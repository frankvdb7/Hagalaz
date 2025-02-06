using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Currencies
{
    /// <summary>
    ///     Item script for tokkul.
    /// </summary>
    [ItemScriptMetaData([6529])]
    public class Tokkul : ItemScript
    {
        /// <summary>
        ///     Get's if specific item can be sold to the specified shop.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool CanSellItem(IItem item, ICharacter character) => false;

        /// <summary>
        ///     Gets the examine.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override string GetExamine(IItem item)
        {
            if (item.Count >= 100000)
            {
                return item.Count + " x Tokkul";
            }

            return "It looks like some kind of coin made from obsidian.";
        }
    }
}