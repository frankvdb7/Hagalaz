using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Features
{
    /// <summary>
    /// An interface that represents an artificial commandprompt.
    /// </summary>
    public interface IGameCommandPrompt
    {
        /// <summary>
        /// Handles a given execute.
        /// </summary>
        /// <param name="command">The name of the command to execute.</param>
        /// <param name="character">The character.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>
        /// Command has been handled
        /// </returns>
        ValueTask<bool> ExecuteAsync(string command, ICharacter character, string[] args);
    }
}
