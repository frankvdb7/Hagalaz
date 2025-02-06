using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Common.Events
{
    /// <summary>
    /// Class CreatureLocationChangedEvent
    /// </summary>
    public class CreatureLocationChangedEvent : CreatureEvent
    {
        /// <summary>
        /// Contains old character location.
        /// </summary>
        /// <value>The old location.</value>
        public ILocation? OldLocation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatureLocationChangedEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="oldLocation">The old location.</param>
        public CreatureLocationChangedEvent(ICreature target, ILocation? oldLocation) : base(target) => OldLocation = oldLocation;
    }
}