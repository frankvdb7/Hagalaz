using Hagalaz.Cache.Types;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Services.GameWorld.Data.Model
{
    /// <summary>
    /// Represents an item's definition.
    /// </summary>
    public class ItemDefinition : ItemType, IItemDefinition
    {
        /// <summary>
        /// Gets the item's examination quote.
        /// </summary>
        public string Examine { get; set; }

        /// <summary>
        /// Contains item weight.
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Contains boolean if item is tradeable.
        /// </summary>
        public bool Tradeable { get; set; }

        /// <summary>
        /// Contains item high alchemy value.
        /// </summary>
        public int HighAlchemyValue { get; set; }

        /// <summary>
        /// Contains item low alchemy value.
        /// </summary>
        public int LowAlchemyValue { get; set; }

        /// <summary>
        /// Contains exhange price price.
        /// </summary>
        public int TradeValue { get; set; }

        /// <summary>
        /// Construct's new item definition.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        public ItemDefinition(int id)
            : base(id)
        {
            Examine = "It's an item.";
            Tradeable = true;
            Weight = 0.0;
            TradeValue = 1;
            HighAlchemyValue = 1;
            LowAlchemyValue = 1;
        }
    }
}