using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Features
{
    /// <summary>
    /// Defines a contract for a game command prompt that processes and executes commands entered by players.
    /// </summary>
    public interface IGameCommandPrompt
    {
        /// <summary>
        /// Asynchronously executes a game command for a specific character.
        /// </summary>
        /// <param name="command">The name of the command to execute (e.g., "teleport", "item").</param>
        /// <param name="character">The character who is executing the command.</param>
        /// <param name="args">An array of string arguments provided with the command.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that resolves to <c>true</c> if the command was successfully handled; otherwise, <c>false</c>.</returns>
        ValueTask<bool> ExecuteAsync(string command, ICharacter character, string[] args);
    }
}
