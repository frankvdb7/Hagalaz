// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Security;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     /// <summary>
//     ///     A packet composer for sending messages between friends chat channel members.
//     /// </summary>
//     public class AddFriendsChatMessageRequestPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Composes a packet for sending messages between friends chat channel members.
//         /// </summary>
//         /// <param name="sessionId">The server session key of the character.</param>
//         /// <param name="message">The message to be sent to the chat channel.</param>
//         public AddFriendsChatMessageRequestPacketComposer(long sessionId, string message)
//         {
//             SetOpcode(AddFriendsChatMessageRequest.Opcode);
//             SetType(SizeType.Short);
//
//             var encoded = Huffman.Encode(message, out var messageLength);
//
//             AppendLong(sessionId);
//             AppendShort((short)messageLength);
//             AppendBytes(encoded);
//         }
//     }
// }