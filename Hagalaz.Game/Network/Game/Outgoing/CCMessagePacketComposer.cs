// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Game.Outgoing
// {
//     /// <summary>
//     ///     Packet for sending messages inside a clans chat channel.
//     /// </summary>
//     public class CcMessagePacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Composes a packet that sends a friends chat channel message.
//         /// </summary>
//         /// <param name="displayName">The name of the character sending the message.</param>
//         /// <param name="myClan">if set to <c>true</c> [my clan].</param>
//         /// <param name="rights">The rights.</param>
//         /// <param name="messageUid">The unique message indentifer.</param>
//         /// <param name="messageLength">Length of the message.</param>
//         /// <param name="encodedMessage">The encoded message.</param>
//         public CcMessagePacketComposer(string displayName, bool myClan, byte rights, long messageUid, short messageLength, byte[] encodedMessage)
//         {
//             SetOpcode(120);
//             SetType(SizeType.Byte);
//
//             AppendByte(myClan ? (byte)1 : (byte)0);
//             AppendString(displayName);
//
//             AppendShort((short)(messageUid >> 32));
//             AppendMedInt((int)(messageUid - ((messageUid >> 32) << 32)));
//             AppendByte(rights);
//             AppendSmart(messageLength);
//             AppendBytes(encodedMessage);
//         }
//     }
// }