// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Security;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     /// <summary>
//     ///     A packet composer for sending messages between clan chat channel members.
//     /// </summary>
//     public class SendClanChatMessagePacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Composes a packet for sending messages between friends chat channel members.
//         /// </summary>
//         /// <param name="masterId">The server session key of the character.</param>
//         /// <param name="clanName"></param>
//         /// <param name="message">The message to be sent to the chat channel.</param>
//         /// <param name="guestChannel">if set to <c>true</c> [guest channel].</param>
//         public SendClanChatMessagePacketComposer(uint masterId, string clanName, string message, bool guestChannel)
//         {
//             SetOpcode(1337);
//             SetType(SizeType.Short);
//
//             char[] ma = message.ToCharArray();
//             ma[0] = char.ToUpper(ma[0]);
//             string upperCaseFirstMessage = new string(ma);
//
//             byte[] encoded = Huffman.Encode(upperCaseFirstMessage, out var messageLength);
//
//             AppendInt((int)masterId);
//             AppendString(clanName);
//             AppendByte(guestChannel ? (byte)1 : (byte)0);
//             AppendSmart((short)messageLength);
//             AppendBytes(encoded);
//         }
//     }
// }