using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for sell allow event.
    /// If at least one event handler will
    /// catch this event selling given item
    /// will not be allowed.
    /// </summary>
    public class SellAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <value>
        /// The item.
        /// </value>
        public IItem Item { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SellAllowEvent" /> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="item">The item.</param>
        public SellAllowEvent(ICharacter c, IItem item) : base(c) => Item = item;
    }
}