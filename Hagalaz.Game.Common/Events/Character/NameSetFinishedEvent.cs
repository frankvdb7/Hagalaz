using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Class for name set event.
    /// </summary>
    public class NameSetFinishedEvent : CharacterEvent
    {
        /// <summary>
        /// Get's if name setting suceeded.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
        public bool Success { get; }

        /// <summary>
        /// Gets the return message.
        /// </summary>
        public string ReturnMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NameSetFinishedEvent" /> class.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <param name="sucess">if set to <c>true</c> [sucess].</param>
        /// <param name="returnMessage">The return message.</param>
        public NameSetFinishedEvent(ICharacter c, bool sucess, string returnMessage) : base(c)
        {
            Success = sucess;
            ReturnMessage = returnMessage;
        }
    }
}