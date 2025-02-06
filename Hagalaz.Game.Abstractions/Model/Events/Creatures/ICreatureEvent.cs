using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Model.Events.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICreatureEvent : IEvent
    {
        /// <summary>
        /// Contains target to which this event happened.
        /// </summary>
        ICreature Target { get; }
    }
}
