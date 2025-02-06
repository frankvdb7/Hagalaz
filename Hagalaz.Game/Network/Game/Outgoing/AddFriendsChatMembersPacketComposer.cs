// using System.Collections.Generic;
// using Hagalaz.Game.Abstractions.Features.FriendsChat;
// using Hagalaz.Game.Network.Model;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Utilities;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Game.Outgoing
// {
//     /// <summary>
//     ///     Composes information for a friends chat channel.
//     /// </summary>
//     public class AddFriendsChatMembersPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Constructs the packet for listing memebers in a friends chat channel.
//         /// </summary>
//         /// <param name="ownerDisplayName">The owner of the friends chat channel.</param>
//         /// <param name="ownerPreviousDisplayName">Display name of the owner previous.</param>
//         /// <param name="alias">The alias name of the chat channel.</param>
//         /// <param name="rankToKick">The require rank to kick in this channel.</param>
//         /// <param name="members">The list of members in this channel.</param>
//         public AddFriendsChatMembersPacketComposer(string ownerDisplayName, string ownerPreviousDisplayName, string alias, FriendsChatRank rankToKick, List<FriendsChatMemberDto> members)
//         {
//             SetOpcode(23);
//             SetType(SizeType.Short);
//
//             AppendString(ownerDisplayName);
//             if (ownerPreviousDisplayName != null)
//             {
//                 AppendByte(1);
//                 AppendString(ownerPreviousDisplayName);
//             }
//             else
//             {
//                 AppendByte(0);
//             }
//
//             AppendLong(alias.StringToLong());
//
//             AppendByte((byte)rankToKick);
//             AppendByte((byte)members.Count);
//             members.ForEach(m =>
//             {
//                 AppendString(m.DisplayName);
//                 if (!string.IsNullOrEmpty(m.PreviousDisplayName))
//                 {
//                     AppendByte(1);
//                     AppendString(m.PreviousDisplayName);
//                 }
//                 else
//                 {
//                     AppendByte(0);
//                 }
//
//                 AppendShort((short)m.WorldId);
//                 AppendByte((byte)m.Rank);
//                 AppendString((m.InLobby ? "Lobby" : "Hagalaz " + m.WorldId));
//             });
//         }
//
//         /// <summary>
//         ///     Composes a packet that indicates leaving/non existant chat channel.
//         /// </summary>
//         public AddFriendsChatMembersPacketComposer()
//         {
//             SetOpcode(23);
//             SetType(SizeType.Short);
//         }
//     }
// }