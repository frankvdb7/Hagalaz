using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Common.Events
{
    /// <summary>
    /// Class CreaturePrayerBonusChangedEvent
    /// </summary>
    public class CreaturePrayerBonusChangedEvent : CreatureEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreaturePrayerBonusChangedEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public CreaturePrayerBonusChangedEvent(ICreature target) : base(target)
        {
        }
    }
}