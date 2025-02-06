using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Common.Events
{
    /// <summary>
    /// 
    /// </summary>
    public class CreatureSetCombatTargetEvent : CreatureEvent
    {
        /// <summary>
        /// Contains the combat target.
        /// </summary>
        public ICreature CombatTarget { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatureSetCombatTargetEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="combatTarget">The combat target.</param>
        public CreatureSetCombatTargetEvent(ICreature target, ICreature combatTarget)
            : base(target) =>
            CombatTarget = combatTarget;
    }
}