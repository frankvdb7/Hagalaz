using Raido.Common.Protocol;

namespace Hagalaz.Game.Abstractions.Model
{
    public interface IGameSession
    {
        /// <summary>
        /// The connection id of the session.
        /// </summary>
        public string ConnectionId { get; init; }
        /// <summary>
        /// The master id of the session.
        /// </summary>
        public uint MasterId { get; init; }

        /// <summary>
        /// Sends the message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        void SendMessage(RaidoMessage message);
    }
}
