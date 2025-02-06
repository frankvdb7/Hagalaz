// using Hagalaz.Game.Network.Model;
// using Hagalaz.Network.Common.Messages;
//
// namespace Hagalaz.Game.Network.Game.Outgoing
// {
//     public class RemoveFriendsChatMemberPacketComposer : PacketComposer
//     {
//         public RemoveFriendsChatMemberPacketComposer(FriendsChatMemberDto member)
//         {
//             SetOpcode(144);
//             SetType(SizeType.Byte);
//
//             AppendString(member.DisplayName);
//             if (!string.IsNullOrEmpty(member.PreviousDisplayName))
//             {
//                 AppendByte(1);
//                 AppendString(member.PreviousDisplayName);
//             }
//             else
//             {
//                 AppendByte(0);
//             }
//
//             AppendShort((short)member.WorldId);
//             AppendByte(unchecked((byte)-128)); // remove flag
//         }
//     }
// }