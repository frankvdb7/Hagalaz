// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     /// <summary>
//     ///     Attempts to request a find and join for the given friends chat channel.
//     /// </summary>
//     public class AddFriendsChatMemberRequestPacketComposer : PacketComposer
//     {
//         public AddFriendsChatMemberRequestPacketComposer(long sessionId, string ownerDisplayName)
//         {
//             SetOpcode(AddFriendsChatMemberRequest.Opcode);
//             SetType(SizeType.Short);
//
//             AppendLong(sessionId);
//             AppendString(ownerDisplayName);
//         }
//     }
// }