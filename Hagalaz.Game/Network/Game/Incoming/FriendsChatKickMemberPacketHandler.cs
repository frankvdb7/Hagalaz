// using System.Threading.Tasks;
// using Hagalaz.Game.Network.Master;
// // using Hagalaz.Network.Common.Messages;
// using Hagalaz.Game.Abstractions.Model;
//
// namespace Hagalaz.Game.Network.Game.Incoming
// {
//     /// <summary>
//     ///     Handles kicking inside friends chat channels.
//     /// </summary>
//     public class FriendsChatKickMemberPacketHandler : IGamePacketHandler
//     {
//         /// <summary>
//         ///     The master connection adapter
//         /// </summary>
//         private readonly IMasterConnectionAdapter _connectionAdapter;
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="FriendsChatKickMemberPacketHandler" /> class.
//         /// </summary>
//         /// <param name="connectionAdapter">The connection adapter.</param>
//         public FriendsChatKickMemberPacketHandler(IMasterConnectionAdapter connectionAdapter) => _connectionAdapter = connectionAdapter;
//
//         /// <summary>
//         ///     Gets or sets the opcode.
//         /// </summary>
//         /// <value>
//         ///     The opcode.
//         /// </value>
//         public byte Opcode => 45;
//
//         /// <summary>
//         ///     Handles the packet.
//         /// </summary>
//         /// <param name="session">The session to handle packet for.</param>
//         /// <param name="packet">The packet containing handle data.</param>
//         // public async Task HandleAsync(IGameSession session, Packet packet)
//         // {
//         //     //if (session.Character.ChatChannels.InFriendsChat)
//         //     //{
//         //     //    string memberToKick = packet.ReadString();
//         //     //    await _connectionAdapter.SendDataAsync(new DoFriendsChatMemberKickRequestPacketComposer(session.Id, memberToKick).Serialize());
//         //     //}
//         // }
//     }
// }