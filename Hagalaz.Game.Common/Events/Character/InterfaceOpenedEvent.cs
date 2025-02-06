using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// 
    /// </summary>
    public class InterfaceOpenedEvent : CharacterEvent
    {
        /// <summary>
        /// Contains the opened interface.
        /// </summary>
        public IWidget Opened { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InterfaceOpenedEvent" /> class.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="opened">The opened.</param>
        public InterfaceOpenedEvent(ICharacter character, IWidget opened)
            : base(character) =>
            Opened = opened;
    }
}