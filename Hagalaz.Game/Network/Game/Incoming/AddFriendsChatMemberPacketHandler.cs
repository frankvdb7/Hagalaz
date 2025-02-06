
//using Hagalaz.Network.Common.Messages;

namespace Hagalaz.Game.Network.Game.Incoming
{
    /// <summary>
    ///     Handles joining friends chat channels.
    /// </summary>
    public class AddFriendsChatMemberPacketHandler : IGamePacketHandler
    {
        /// <summary>
        ///     Gets or sets the opcode.
        /// </summary>
        /// <value>
        ///     The opcode.
        /// </value>
        public byte Opcode => 84;

        /// <summary>
        ///     Handles the packet.
        /// </summary>
        /// <param name="session">The session to handle packet for.</param>
        /// <param name="packet">The packet containing handle data.</param>
        // public async Task HandleAsync(IGameSession session, Packet packet)
        // {
        //     await Task.CompletedTask;
        //     if (packet.RemainingAmount <= 1)
        //     {
        //         //session.Character.ChatChannels.LeaveFriendsChat();
        //     }
        //     else
        //     {
        //         //session.Character.SendChatMessage("Attempting to join channel...");
        //         //session.Character.ChatChannels.JoinFriendsChat(packet.ReadString());
        //     }
        // }
    }
}