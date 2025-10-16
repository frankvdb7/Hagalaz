namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for a chat message request that can be sent.
    /// </summary>
    public interface IChatMessageRequest
    {
        /// <summary>
        /// Attempts to send the chat message.
        /// </summary>
        void TrySend();
    }
}