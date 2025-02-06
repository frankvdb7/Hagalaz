using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Utilities;

namespace Hagalaz.Game.Scripts.Items.Currencies
{
    /// <summary>
    ///     Item script for coins.
    /// </summary>
    [ItemScriptMetaData([995])]
    public class Coins : ItemScript
    {
        /// <summary>
        ///     Items the clicked in inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.Option7Click)
            {
                character.MoneyPouch.AddFromInventory(item.Count);
            }
            else
            {
                base.ItemClickedInInventory(clickType, item, character);
            }
        }

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
                return StringUtilities.FormatNumber(item.Count) + " x Coins";
            }

            return "Lovely money!";
        }
    }
}