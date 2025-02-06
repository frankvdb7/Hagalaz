using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for sell allow event.
    /// If at least one event handler will
    /// catch this event buying the given item
    /// will not be allowed.
    /// </summary>
    public class ShopItemBoughtEvent : CharacterEvent
    {
        /// <summary>
        /// Gets the shop.
        /// </summary>
        /// <value>
        /// The shop.
        /// </value>
        public IShop Shop { get; }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <value>
        /// The item.
        /// </value>
        public IItem Item { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShopItemBoughtEvent"/> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="shop">The shop.</param>
        /// <param name="item">The item.</param>
        public ShopItemBoughtEvent(ICharacter c, IShop shop, IItem item)
            : base(c)
        {
            Shop = shop;
            Item = item;
        }
    }
}