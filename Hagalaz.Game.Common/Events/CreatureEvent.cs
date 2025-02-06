using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Events.Creatures;

namespace Hagalaz.Game.Common.Events
{
    /// <summary>
    /// Event which happened to specific character.
    /// </summary>
    public abstract class CreatureEvent : Event, ICreatureEvent
    {
        /// <summary>
        /// Contains target to which this event happened.
        /// </summary>
        public ICreature Target { get; }

        /// <summary>
        /// Construct's new character event with given target.
        /// </summary>
        /// <param name="target">Creature to which this event happened.</param>
        protected CreatureEvent(ICreature target) => Target = target;
    }
}