using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class BankChangedEvent
    /// </summary>
    public class BankChangedEvent : CharacterEvent
    {
        /// <summary>
        /// Contains slots which were changed or null
        /// if all slots changed.
        /// </summary>
        /// <value>The changed slots.</value>
        public HashSet<int>? ChangedSlots { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BankChangedEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="changedSlots">The changed slots.</param>
        public BankChangedEvent(ICharacter target, HashSet<int>? changedSlots = null) : base(target) => ChangedSlots = changedSlots;

    }
}