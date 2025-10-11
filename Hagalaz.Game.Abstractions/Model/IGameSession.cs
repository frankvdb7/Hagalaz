using Raido.Common.Protocol;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Defines the contract for a player's game session, which represents their active connection to the game world.
    /// </summary>
    public interface IGameSession
    {
        /// <summary>
        /// Gets the unique identifier for the underlying network connection.
        /// </summary>
        public string ConnectionId { get; init; }

        /// <summary>
        /// Gets the unique identifier for the player's master account.
        /// </summary>
        public uint MasterId { get; init; }

        /// <summary>
        /// Sends a message to the client associated with this game session.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        void SendMessage(RaidoMessage message);
    }
}
