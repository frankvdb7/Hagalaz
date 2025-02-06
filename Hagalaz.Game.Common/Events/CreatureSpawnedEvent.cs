using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Common.Events
{
    /// <summary>
    /// Class CreatureSpawnedEvent
    /// </summary>
    public class CreatureSpawnedEvent : CreatureEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatureSpawnedEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public CreatureSpawnedEvent(ICreature target) : base(target)
        {
        }
    }
}