// using Hagalaz.Network.Common.Messages;
//
// namespace Hagalaz.Game.Network.Game.Outgoing
// {
//     /// <summary>
//     ///     Packet for minimap flag.
//     /// </summary>
//     public class MinimapFlagPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Constructs new packet.
//         /// </summary>
//         /// <param name="mapX">The map x.</param>
//         /// <param name="mapY">The map y.</param>
//         public MinimapFlagPacketComposer(byte mapX, byte mapY)
//         {
//             SetOpcode(101);
//             AppendByteA(mapY);
//             AppendByteC(mapX);
//         }
//     }
// }