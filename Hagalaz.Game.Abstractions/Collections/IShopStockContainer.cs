using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Collections
{
    /// <summary>
    /// 
    /// </summary>
    public interface IShopStockContainer : IItemContainer
    {
        /// <summary>
        /// Normalizes the stock.
        /// </summary>
        void NormalizeStock();
        /// <summary>
        /// Sets the internal items array with the given array.
        /// </summary>
        /// <param name="items">An array of Item objects.</param>
        /// <param name="update">if set to <c>true</c> [update].</param>
        void SetItems(IItem[] items, bool update);
        /// <summary>
        /// Sells specific item to the shop.
        /// </summary>
        /// <param name="viewer">The viewer.</param>
        /// <param name="item">Item which should be deposited.</param>
        /// <param name="count">The count.</param>
        /// <returns>If depositing was sucessfull.</returns>
        bool SellFromInventory(ICharacter viewer, IItem item, int count);
        /// <summary>
        /// Buys from shop.
        /// </summary>
        /// <param name="viewer">The viewer.</param>
        /// <param name="item">The item.</param>
        /// <param name="count">The count.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool BuyFromShop(ICharacter viewer, IItem item, int count);
    }
}
