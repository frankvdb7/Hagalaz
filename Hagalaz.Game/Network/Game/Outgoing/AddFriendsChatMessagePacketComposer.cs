// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Utilities;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Game.Outgoing
// {
//     /// <summary>
//     ///     Packet for sending messages inside a friends chat channel.
//     /// </summary>
//     public class AddFriendsChatMessagePacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Composes a packet that sends a friends chat channel message.
//         /// </summary>
//         /// <param name="displayName">The name of the character sending the message.</param>
//         /// <param name="previousDisplayName">Display name of the previous.</param>
//         /// <param name="chatName">The clan's alias name.</param>
//         /// <param name="rights">The rights.</param>
//         /// <param name="messageUid">The unique message indentifer.</param>
//         /// <param name="messageLength">Length of the message.</param>
//         /// <param name="encodedMessage">The encoded message.</param>
//         public AddFriendsChatMessagePacketComposer(string displayName, string previousDisplayName, string chatName, byte rights, long messageUid, short messageLength, byte[] encodedMessage)
//         {
//             SetOpcode(86);
//             SetType(SizeType.Byte);
//
//             // packet structure
//             bool hasPreviousDisplayName = !string.IsNullOrEmpty(previousDisplayName);
//             AppendByte(hasPreviousDisplayName ? (byte)1 : (byte)0); // speaker has previous display name.
//             AppendString(displayName);
//             if (hasPreviousDisplayName)
//             {
//                 AppendString(previousDisplayName);
//             }
//
//             AppendLong(chatName.StringToLong());
//             AppendShort((short)(messageUid >> 32));
//             AppendMedInt((int)(messageUid - ((messageUid >> 32) << 32)));
//             AppendByte(rights);
//             AppendSmart(messageLength);
//             AppendBytes(encodedMessage);
//         }
//     }
// }