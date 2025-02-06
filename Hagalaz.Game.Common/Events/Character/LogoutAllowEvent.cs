using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for logout allow event.
    /// </summary>
    public class LogoutAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogoutAllowEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        public LogoutAllowEvent(ICharacter target) : base(target)
        {
        }
    }
}