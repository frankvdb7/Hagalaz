using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// Defines the contract for a shop's stock container, which manages the items available for sale
    /// and handles buy/sell transactions with players.
    /// </summary>
    public interface IShopStockContainer : IItemContainer
    {
        /// <summary>
        /// Resets the shop's stock to its default state. This is typically called periodically
        /// to replenish items and remove excess player-sold items.
        /// </summary>
        void NormalizeStock();

        /// <summary>
        /// Replaces the entire shop stock with a new set of items.
        /// </summary>
        /// <param name="items">The new array of items for the shop's stock.</param>
        /// <param name="update">If set to <c>true</c>, an update callback is invoked to refresh the view for any players viewing the shop.</param>
        void SetItems(IItem[] items, bool update);

        /// <summary>
        /// Sells an item from a character's inventory to this shop.
        /// </summary>
        /// <param name="viewer">The character who is selling the item.</param>
        /// <param name="item">The item from the character's inventory to be sold.</param>
        /// <param name="count">The number of items to sell.</param>
        /// <returns><c>true</c> if the sale was successful; otherwise, <c>false</c>.</returns>
        bool SellFromInventory(ICharacter viewer, IItem item, int count);

        /// <summary>
        /// Buys an item from this shop's stock.
        /// </summary>
        /// <param name="viewer">The character who is buying the item.</param>
        /// <param name="item">The item in the shop's stock to be bought.</param>
        /// <param name="count">The number of items to buy.</param>
        /// <returns><c>true</c> if the purchase was successful; otherwise, <c>false</c>.</returns>
        bool BuyFromShop(ICharacter viewer, IItem item, int count);
    }
}
