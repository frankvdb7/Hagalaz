using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// 
    /// </summary>
    public class MoneyPouchChangedEvent : CharacterEvent
    {
        /// <summary>
        /// Contains the previous amount.
        /// </summary>
        public int PreviousCount { get; }

        /// <summary>
        /// Contains the (new) amount.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryChangedEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="previousCount">The previous amount.</param>
        /// <param name="count">The amount.</param>
        public MoneyPouchChangedEvent(ICharacter target, int previousCount, int count)
            : base(target)
        {
            PreviousCount = previousCount;
            Count = count;
        }
    }
}