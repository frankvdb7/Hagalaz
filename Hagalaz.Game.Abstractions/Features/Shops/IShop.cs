using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Features.Shops
{
    /// <summary>
    /// Defines the contract for a shop, which manages its own stock, currency, and pricing.
    /// </summary>
    public interface IShop
    {
        /// <summary>
        /// Gets the name of the shop.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the item ID of the currency used in this shop (e.g., coins, tokens).
        /// </summary>
        int CurrencyId { get; }

        /// <summary>
        /// Gets a value indicating whether this is a general store, which typically buys any item from players.
        /// </summary>
        bool GeneralStore { get; }

        /// <summary>
        /// Gets the container that holds the main stock of the shop, which can fluctuate based on player transactions.
        /// </summary>
        IShopStockContainer MainStockContainer { get; }

        /// <summary>
        /// Gets the container that holds a sample of the shop's default stock. This is used to determine what items the shop will buy back.
        /// </summary>
        IShopStockContainer SampleStockContainer { get; }

        /// <summary>
        /// Calculates the value a player receives for selling a specific item to this shop.
        /// </summary>
        /// <param name="item">The item to be sold.</param>
        /// <returns>The sell value in the shop's currency.</returns>
        int GetSellValue(IItem item);

        /// <summary>
        /// Calculates the cost for a player to buy a specific item from this shop.
        /// </summary>
        /// <param name="item">The item to be bought.</param>
        /// <returns>The buy value in the shop's currency.</returns>
        int GetBuyValue(IItem item);
    }
}
