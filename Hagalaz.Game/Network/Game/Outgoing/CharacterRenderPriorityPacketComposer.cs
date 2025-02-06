// using Hagalaz.Network.Common.Messages;
//
// namespace Hagalaz.Game.Network.Game.Outgoing
// {
//     /// <summary>
//     /// </summary>
//     public class CharacterRenderPriorityPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="CharacterRenderPriorityPacketComposer" /> class.
//         /// </summary>
//         /// <param name="renderUnderNpcs">if set to <c>true</c> [render under NPCS].</param>
//         public CharacterRenderPriorityPacketComposer(bool renderUnderNpcs)
//         {
//             SetOpcode(73);
//             AppendByte(renderUnderNpcs ? (byte)1 : (byte)0);
//         }
//     }
// }