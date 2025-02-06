using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Abstract class for character event.
    /// </summary>
    public abstract class CharacterEvent : CreatureEvent
    {
        /// <summary>
        /// Contains target to which this event happened.
        /// </summary>
        public new ICharacter Target { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        protected CharacterEvent(ICharacter target) : base(target) => Target = target;
    }
}