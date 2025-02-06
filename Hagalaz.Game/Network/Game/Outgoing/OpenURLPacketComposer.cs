// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Game.Outgoing
// {
//     /// <summary>
//     ///     Composes a packet that opens a URL for the specific character.
//     /// </summary>
//     public class OpenUrlPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="OpenUrlPacketComposer" /> class.
//         /// </summary>
//         /// <param name="url">The URL.</param>
//         public OpenUrlPacketComposer(string url)
//         {
//             SetOpcode(61);
//             SetType(SizeType.Short);
//
//             AppendByte(0); // bool
//             // if bool
//             // appendString
//             AppendString(url);
//         }
//     }
// }