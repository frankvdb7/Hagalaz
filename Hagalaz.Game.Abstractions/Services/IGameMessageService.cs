using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGameMessageService
    {
        /// <summary>
        /// Announces the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="type">The type.</param>
        /// <param name="announcerDisplayName">Display name of the announcer.</param>
        Task MessageAsync(string message, GameMessageType type, string? announcerDisplayName = null);
    }
}