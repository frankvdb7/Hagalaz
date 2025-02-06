using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Common.Events
{
    /// <summary>
    /// Event which happens when specific creature is killed.
    /// </summary>
    public class CreatureKillEvent : CreatureEvent
    {
        /// <summary>
        /// Contains object which performed the interruption,
        /// this parameter can be null , but it is not encouraged to do so.
        /// Best use would be to set the invoker class instance as source.
        /// </summary>
        /// <value>The source.</value>
        public ICreature Victim { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatureInterruptedEvent" /> class.
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <param name="victim">The killed.</param>
        public CreatureKillEvent(ICreature killer, ICreature victim)
            : base(killer) =>
            Victim = victim;
    }
}