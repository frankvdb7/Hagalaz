using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for chat allow event.
    /// If at least one event handler will
    /// catch this event chating given text
    /// will not be allowed.
    /// </summary>
    public class ChatAllowEvent : CharacterEvent
    {
        /// <summary>
        /// Contains text that is about to be chated.
        /// </summary>
        /// <value>The text.</value>
        public string Text { get; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ChatAllowEvent" /> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="text">The text.</param>
        /// <param name="effects">The effects.</param>
        public ChatAllowEvent(ICharacter c, string text) : base(c) => Text = text;
    }
}