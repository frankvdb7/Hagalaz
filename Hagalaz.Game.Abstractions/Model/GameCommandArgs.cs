using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class GameCommandArgs : HandledEventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public ICharacter Character { get; private set; }
        
        /// <summary>
        /// 
        /// </summary>
        public string[] Arguments { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="arguments"></param>
        public GameCommandArgs(ICharacter character, string[] arguments)
        {
            Character = character;
            Arguments = arguments;
        }
    }
}