using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// 
    /// </summary>
    public interface IItemDefinition : IItemType
    {
        /// <summary>
        /// Gets the item's examination quote.
        /// </summary>
        string Examine { get; set; }
        /// <summary>
        /// Contains item weight.
        /// </summary>
        double Weight { get; set; }
        /// <summary>
        /// Contains boolean if item is tradeable.
        /// </summary>
        bool Tradeable { get; set; }
        /// <summary>
        /// Contains item high alchemy value.
        /// </summary>
        int HighAlchemyValue { get; set; }
        /// <summary>
        /// Contains item low alchemy value.
        /// </summary>
        int LowAlchemyValue { get; set; }
        /// <summary>
        /// Contains exhange price price.
        /// </summary>
        int TradeValue { get; set; }
    }
}
