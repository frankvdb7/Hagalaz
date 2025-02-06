using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// 
    /// </summary>
    public class EquipmentChangedEvent : CharacterEvent
    {
        /// <summary>
        /// Contains slots which were changed.
        /// </summary>
        public HashSet<EquipmentSlot>? ChangedSlots { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EquipmentChangedEvent"/> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="changedSlots">The changed slots.</param>
        public EquipmentChangedEvent(ICharacter target, HashSet<EquipmentSlot>? changedSlots = null) : base(target) => ChangedSlots = changedSlots;
    }
}