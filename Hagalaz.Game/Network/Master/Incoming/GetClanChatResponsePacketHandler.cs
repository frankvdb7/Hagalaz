// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Hagalaz.Game.Abstractions.Features.Clans;
// using Hagalaz.Game.Features.Clans;
// using Hagalaz.Game.Network.Game.Outgoing;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Game.Abstractions.Store;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     /// <summary>
//     ///     Handles a given list of friends chat members, and sends it to the client.
//     /// </summary>
//     public class GetClanChatResponsePacketHandler : IMasterPacketHandler
//     {
//         /// <summary>
//         ///     The character repository
//         /// </summary>
//         private readonly ICharacterStore _characterStore;
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="GetClanChatResponsePacketHandler" /> class.
//         /// </summary>
//         /// <param name="characterStore">The character repository.</param>
//         public GetClanChatResponsePacketHandler(ICharacterStore characterStore) => _characterStore = characterStore;
//
//         /// <summary>
//         ///     Gets the opcode.
//         /// </summary>
//         /// <value>
//         ///     The opcode.
//         /// </value>
//         public int Opcode => 53121;
//
//         /// <summary>
//         ///     Handles the packet.
//         /// </summary>
//         /// <param name="packet">The packet containing handle data.</param>
//         public async Task HandleAsync(Packet packet)
//         {
//             List<CcMembersListPacketComposer> packets = new List<CcMembersListPacketComposer>();
//             List<IClanMember> members = new List<IClanMember>();
//
//             string clanName = packet.ReadString();
//             ClanRank rankToKick = (ClanRank)packet.ReadByte();
//             ClanRank rankToTalk = (ClanRank)packet.ReadByte();
//
//             short memberCount = packet.ReadShort();
//             for (int i = 0; i < memberCount; i++)
//             {
//                 uint masterId = (uint)packet.ReadInt();
//                 string displayName = packet.ReadString();
//                 short worldId = packet.ReadShort();
//                 bool inLobby = packet.ReadByte() == 1;
//                 ClanRank rank = (ClanRank)packet.ReadByte();
//                 members.Add(new ClanMember()
//                 {
//                     MasterId = masterId,
//                     DisplayName = displayName,
//                     WorldId = worldId,
//                     InLobby = inLobby,
//                     Rank = rank
//                 });
//
//                 byte online = packet.ReadByte();
//                 if (online != 1)
//                 {
//                     continue;
//                 }
//
//                 long key = packet.ReadLong();
//                 //var character = await _characterStore.FindAllAsync().Where(c => c.Session.Id == key).SingleOrDefaultAsync();
//
//                 //if (character == null)
//                 //{
//                 //    continue;
//                 //}
//
//                 //packets.Add(new CcMembersListPacketComposer(character, rank > ClanRank.Guest));
//                 //if (rank == ClanRank.Guest)
//                 //{
//                 //    character.Configurations.SendCs2Script(4453, new object[]
//                 //    {
//                 //    });
//                 //}
//             }
//
//             await Task.WhenAll(packets.Select(p => p.SendAsync(clanName, rankToKick, rankToTalk, members)));
//         }
//     }
// }