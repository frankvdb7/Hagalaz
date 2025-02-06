using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Common.Events
{
    /// <summary>
    /// Event which happens when specific creature is interrupted.
    /// </summary>
    public class CreatureInterruptedEvent : CreatureEvent
    {
        /// <summary>
        /// Contains object which performed the interruption,
        /// this parameter can be null , but it is not encouraged to do so.
        /// Best use would be to set the invoker class instance as source.
        /// </summary>
        /// <value>The source.</value>
        public object Source { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatureInterruptedEvent" /> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="source">The source.</param>
        public CreatureInterruptedEvent(ICreature c, object source) : base(c) => Source = source;
    }
}