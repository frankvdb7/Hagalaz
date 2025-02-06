// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Game.Outgoing
// {
//     /// <summary>
//     ///     Packet composer.
//     /// </summary>
//     public class SetWalkhereOptionPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Constructs new packet.
//         /// </summary>
//         /// <param name="text">The text.</param>
//         /// <param name="cursorSpriteID">The cursor sprite identifier.</param>
//         public SetWalkhereOptionPacketComposer(string text, short cursorSpriteID)
//         {
//             SetOpcode(88);
//             SetType(SizeType.Byte);
//             AppendString(text);
//             AppendShort(cursorSpriteID);
//         }
//     }
// }