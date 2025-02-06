
//using Hagalaz.Network.Common.Messages;

namespace Hagalaz.Game.Network.Game
{
    /// <summary>
    ///     Defines a handler for packets.
    /// </summary>
    public interface IGamePacketHandler
    {
        /// <summary>
        ///     Gets or sets the opcode.
        /// </summary>
        /// <value>
        ///     The opcode.
        /// </value>
        byte Opcode { get; }

        /// <summary>
        ///     Handles the asynchronous game packet.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="packet">The packet.</param>
        /// <returns></returns>
        //Task HandleAsync(IGameSession session, Packet packet);
    }
}