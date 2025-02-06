// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Network.Common.Messages;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     public class GetFriendsChatRequestPacketComposer : PacketComposer
//     {
//         public GetFriendsChatRequestPacketComposer(long sessionId)
//         {
//             SetOpcode(GetFriendsChatRequest.Opcode);
//             SetType(SizeType.Short);
//             AppendLong(sessionId);
//         }
//     }
// }