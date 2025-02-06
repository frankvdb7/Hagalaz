using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Features.Shops
{
    /// <summary>
    /// 
    /// </summary>
    public interface IShop
    {
        /// <summary>
        /// The name of the shop.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
        /// <summary>
        /// Gets the currency identifier.
        /// </summary>
        /// <value>
        /// The currency identifier.
        /// </value>
        int CurrencyId { get; }
        /// <summary>
        /// Gets a value indicating whether [general store].
        /// </summary>
        /// <value><c>true</c> if [general store]; otherwise, <c>false</c>.</value>
        bool GeneralStore { get; }
        /// <summary>
        /// The shop container that holds the main stock.
        /// </summary>
        /// <value>The main stock container.</value>
        IShopStockContainer MainStockContainer { get; }
        /// <summary>
        /// The shop container that holds the main stock.
        /// </summary>
        /// <value>The sample stock container.</value>
        IShopStockContainer SampleStockContainer { get; }
        /// <summary>
        /// Gets the sell value for the item in this shop.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        int GetSellValue(IItem item);
        /// <summary>
        /// Gets the buy value for the item in this shop.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        int GetBuyValue(IItem item);
    }
}
