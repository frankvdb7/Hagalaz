using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Provides data for a game command event, including the character who executed the command and the provided arguments.
    /// </summary>
    public class GameCommandArgs : HandledEventArgs
    {
        /// <summary>
        /// Gets the character who executed the command.
        /// </summary>
        public ICharacter Character { get; private set; }
        
        /// <summary>
        /// Gets the arguments provided with the command.
        /// </summary>
        public string[] Arguments { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameCommandArgs"/> class.
        /// </summary>
        /// <param name="character">The character who executed the command.</param>
        /// <param name="arguments">The arguments provided with the command.</param>
        public GameCommandArgs(ICharacter character, string[] arguments)
        {
            Character = character;
            Arguments = arguments;
        }
    }
}