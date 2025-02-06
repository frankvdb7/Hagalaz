using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// 
    /// </summary>
    public class InventoryChangedEvent : CharacterEvent
    {
        /// <summary>
        /// Contains slots which were changed.
        /// </summary>
        public HashSet<int>? ChangedSlots { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryChangedEvent"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="changedSlots">The changed slots.</param>
        public InventoryChangedEvent(ICharacter target, HashSet<int>? changedSlots = null) : base(target) => ChangedSlots = changedSlots;

    }
}