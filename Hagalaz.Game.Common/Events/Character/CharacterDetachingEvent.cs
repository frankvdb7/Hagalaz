using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Raised synchronously while a character detaches, before the final logout snapshot is captured.
    /// Handlers may finalize only synchronous domain state that must be part of that snapshot.
    /// </summary>
    public class CharacterDetachingEvent : CharacterEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterDetachingEvent"/> class.
        /// </summary>
        /// <param name="target">The detaching character.</param>
        public CharacterDetachingEvent(ICharacter target) : base(target) { }
    }
}
