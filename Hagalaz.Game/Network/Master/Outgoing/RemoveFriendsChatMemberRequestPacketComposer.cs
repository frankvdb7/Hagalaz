// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     public class RemoveFriendsChatMemberRequestPacketComposer : PacketComposer
//     {
//         public RemoveFriendsChatMemberRequestPacketComposer(long sessionId)
//         {
//             SetOpcode(RemoveFriendsChatMemberRequest.Opcode);
//             SetType(SizeType.Short);
//
//             AppendLong(sessionId);
//         }
//     }
// }