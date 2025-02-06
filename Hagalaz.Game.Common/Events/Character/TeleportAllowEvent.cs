using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for teleport allow event.
    /// If at least one event handler will
    /// catch this event teleporting will not be allowed.
    /// </summary>
    public class TeleportAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Contains target location.
        /// </summary>
        /// <value>The target location.</value>
        public ILocation TargetLocation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TeleportAllowEvent" /> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="target">The target.</param>
        public TeleportAllowEvent(ICharacter c, ILocation target) : base(c) => TargetLocation = target;
    }
}