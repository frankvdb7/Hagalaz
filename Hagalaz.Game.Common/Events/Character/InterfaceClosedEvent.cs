using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// 
    /// </summary>
    public class InterfaceClosedEvent : CharacterEvent
    {
        /// <summary>
        /// Contains the closed interface.
        /// </summary>
        public IWidget Closed { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceClosedEvent" /> class.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="closed">The closed.</param>
        public InterfaceClosedEvent(ICharacter character, IWidget closed)
            : base(character) =>
            Closed = closed;
    }
}