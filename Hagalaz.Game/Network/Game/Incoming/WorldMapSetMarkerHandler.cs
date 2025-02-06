
// using Hagalaz.Network.Common.Messages;

namespace Hagalaz.Game.Network.Game.Incoming
{
    /// <summary>
    /// </summary>
    public class WorldMapSetMarkerHandler : IGamePacketHandler
    {
        /// <summary>
        ///     Gets or sets the opcode.
        /// </summary>
        /// <value>
        ///     The opcode.
        /// </value>
        public byte Opcode => 66;

        /// <summary>
        ///     Handles the packet.
        /// </summary>
        /// <param name="session">The session to handle packet for.</param>
        /// <param name="packet">The packet containing handle data.</param>
        // public Task HandleAsync(IGameSession session, Packet packet)
        // {
        //     //new WorldMapSetMarkerEvent(session.Character, packet.ReadIntSecondary()).Send();
        //     return Task.CompletedTask;
        // }
    }
}