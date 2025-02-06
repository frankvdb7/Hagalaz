using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.Shops;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    ///
    /// </summary>
    public class ShopStockChangedEvent : Event
    {
        /// <summary>
        /// Gets the shop.
        /// </summary>
        public IShop Shop { get; }

        /// <summary>
        /// Contains colorSlots which were changed.
        /// </summary>
        public HashSet<int>? ChangedSlots { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShopStockChangedEvent" /> class.
        /// </summary>
        /// <param name="shop">The shop.</param>
        /// <param name="changedSlots">The changed colorSlots.</param>
        public ShopStockChangedEvent(IShop shop, HashSet<int>? changedSlots = null)
        {
            Shop = shop;
            ChangedSlots = changedSlots;
        }
    }
}