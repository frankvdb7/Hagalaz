using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for summoning allow event.
    /// If at least one event handler will
    /// catch this event summoning 
    /// will not be allowed.
    /// </summary>
    public class SummoningAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SummoningAllowEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public SummoningAllowEvent(ICharacter target) : base(target)
        {
        }
    }
}