using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that broadcasts global messages to all players in the game world.
    /// </summary>
    public interface IGameMessageService
    {
        /// <summary>
        /// Sends a message to all players.
        /// </summary>
        /// <param name="message">The content of the message.</param>
        /// <param name="type">The type of message, which may affect its color or formatting.</param>
        /// <param name="announcerDisplayName">The optional display name of the announcer.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task MessageAsync(string message, GameMessageType type, string? announcerDisplayName = null);
    }
}