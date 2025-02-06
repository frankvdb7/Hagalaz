using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Common.Events
{
    /// <summary>
    /// 
    /// </summary>
    public class CreatureDestroyedEvent : CreatureEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatureDestroyedEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public CreatureDestroyedEvent(ICreature target) : base(target)
        {
        }
    }
}