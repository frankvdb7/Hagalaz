// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Hagalaz.Game.Abstractions.Features.Clans;
// using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Game.Outgoing
// {
//     /// <summary>
//     ///     Composes information for a clan chat channel.
//     /// </summary>
//     public class CcMembersListPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     The character
//         /// </summary>
//         private readonly ICharacter _character;
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="CcMembersListPacketComposer" /> class.
//         /// </summary>
//         /// <param name="character">The character.</param>
//         /// <param name="myClan">if set to <c>true</c> [my clan].</param>
//         public CcMembersListPacketComposer(ICharacter character, bool myClan)
//         {
//             _character = character;
//             SetOpcode(123);
//             SetType(SizeType.Short);
//             AppendByte(myClan ? (byte)1 : (byte)0);
//         }
//
//         /// <summary>
//         ///     Composes a packet that indicates leaving/non existant clan.
//         /// </summary>
//         /// <param name="myClan">if set to <c>true</c> [my clan].</param>
//         public CcMembersListPacketComposer(bool myClan)
//         {
//             SetOpcode(123);
//             SetType(SizeType.Short);
//             AppendByte(myClan ? (byte)1 : (byte)0);
//         }
//
//         /// <summary>
//         ///     Sends this instance.
//         /// </summary>
//         /// <param name="clanName">Name of the clan.</param>
//         /// <param name="rankToKick">The rank to kick.</param>
//         /// <param name="rankToTalk">The rank to talk.</param>
//         /// <param name="members">The members.</param>
//         public async Task SendAsync(string clanName, ClanRank rankToKick, ClanRank rankToTalk, List<IClanMember> members)
//         {
//             AppendByte(0x2); // read name as string, 0x1 for long
//             AppendLong(4062231702422979939L); // uid - TODO get from clan
//             AppendLong(0); // some update number
//             AppendString(clanName);
//             AppendByte(0); // not used?
//             AppendByte((byte)rankToKick);
//             AppendByte((byte)rankToTalk); // TODO - Guests can talk = -1
//
//             AppendShort((short)members.Count);
//             members.ForEach(m =>
//             {
//                 AppendString(m.DisplayName);
//                 AppendByte((byte)m.Rank);
//                 AppendShort((short)(m.InLobby ? m.WorldId + 1099 : m.WorldId)); // lobby >= 1100 && <= 5001
//             });
//
//             // _character.Session.SendPacketAsync(this);
//         }
//     }
// }