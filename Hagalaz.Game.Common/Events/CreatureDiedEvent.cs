using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Common.Events
{
    /// <summary>
    /// Class CreatureDiedEvent
    /// </summary>
    public class CreatureDiedEvent : CreatureEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreatureDiedEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public CreatureDiedEvent(ICreature target) : base(target)
        {
        }
    }
}