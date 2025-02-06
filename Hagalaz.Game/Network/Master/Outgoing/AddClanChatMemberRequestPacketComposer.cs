// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     /// <summary>
//     ///     Attempts to request a find and join for the given clan chat channel.
//     /// </summary>
//     public class AddClanChatMemberRequestPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Composes a packet that finds and joins a clan chat channel.
//         /// </summary>
//         /// <param name="masterId">The server session key for the character looking for the chat channel.</param>
//         /// <param name="clanName">Name of the clan.</param>
//         /// <param name="guestChannel">if set to <c>true</c> [guest channel].</param>
//         public AddClanChatMemberRequestPacketComposer(uint masterId, string clanName, bool guestChannel)
//         {
//             SetOpcode(AddClanChatMemberRequest.Opcode);
//             SetType(SizeType.Short);
//
//             AppendInt((int)masterId);
//             AppendByte(guestChannel ? (byte)1 : (byte)0);
//             AppendString(clanName);
//         }
//     }
// }