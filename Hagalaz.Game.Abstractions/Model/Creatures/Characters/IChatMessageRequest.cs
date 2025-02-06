namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    public interface IChatMessageRequest
    {
        /// <summary>
        /// Sends this instance.
        /// </summary>
        void TrySend();
    }
}