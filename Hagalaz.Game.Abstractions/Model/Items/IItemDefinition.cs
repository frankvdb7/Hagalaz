using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// Defines the contract for an item's data definition, containing its base properties such as name, value, and stackability.
    /// </summary>
    public interface IItemDefinition : IItemType
    {
        /// <summary>
        /// Gets or sets the item's "Examine" text.
        /// </summary>
        string Examine { get; set; }

        /// <summary>
        /// Gets or sets the weight of the item.
        /// </summary>
        double Weight { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this item is tradeable between players.
        /// </summary>
        bool Tradeable { get; set; }

        /// <summary>
        /// Gets or sets the value of the item when using the High Level Alchemy spell.
        /// </summary>
        int HighAlchemyValue { get; set; }

        /// <summary>
        /// Gets or sets the value of the item when using the Low Level Alchemy spell.
        /// </summary>
        int LowAlchemyValue { get; set; }

        /// <summary>
        /// Gets or sets the item's base trade value, used in shops and the Grand Exchange.
        /// </summary>
        int TradeValue { get; set; }
    }
}