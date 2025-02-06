// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Network.Common.Messages;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     public class DoFriendsChatMemberKickRequestPacketComposer : PacketComposer
//     {
//         public DoFriendsChatMemberKickRequestPacketComposer(long sessionId, string memberDisplayName)
//         {
//             SetOpcode(DoFriendsChatMemberKickRequest.Opcode);
//             SetType(SizeType.Short);
//
//             AppendLong(sessionId);
//             AppendString(memberDisplayName);
//         }
//     }
// }