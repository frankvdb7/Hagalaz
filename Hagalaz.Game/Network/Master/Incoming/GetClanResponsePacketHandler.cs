// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Hagalaz.Game.Features.Clans;
// using Hagalaz.Game.Abstractions.Features.Clans;
// using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Game.Abstractions.Store;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     /// <summary>
//     ///     Handler for clan packet.
//     /// </summary>
//     public class GetClanResponsePacketHandler : IMasterPacketHandler
//     {
//         /// <summary>
//         ///     The character repository
//         /// </summary>
//         private readonly ICharacterStore _characterStore;
//
//         private readonly IClanService _clanManager;
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="GetClanResponsePacketHandler" /> class.
//         /// </summary>
//         /// <param name="characterStore">The character repository.</param>
//         /// <param name="clanManager"></param>
//         public GetClanResponsePacketHandler(ICharacterStore characterStore, IClanService clanManager)
//         {
//             _characterStore = characterStore;
//             _clanManager = clanManager;
//         }
//
//         /// <summary>
//         ///     Gets the opcode.
//         /// </summary>
//         /// <value>
//         ///     The opcode.
//         /// </value>
//         public int Opcode => 12412412;
//
//         /// <summary>
//         ///     Handles the packet.
//         /// </summary>
//         /// <param name="packet">The packet containing handle data.</param>
//         public async Task HandleAsync(Packet packet)
//         {
//             List<ICharacter> characters = new List<ICharacter>();
//             Dictionary<uint, IClanMember> members = new Dictionary<uint, IClanMember>(Clan.MaxMembers);
//             Dictionary<uint, string> bannedMembers = new Dictionary<uint, string>(Clan.MaxBannedMembers);
//
//             string clanName = packet.ReadString();
//             short memberCount = packet.ReadShort();
//             for (int i = 0; i < memberCount; i++)
//             {
//
//                 byte worldMember = packet.ReadByte();
//                 ////if (worldMember == 1)
//                 ////{
//                 ////    long key = packet.ReadLong();
//                 ////    var character = await _characterStore.FindAllAsync().Where(c => c.Session.Id == key).SingleOrDefaultAsync();
//                 ////    if (character != null)
//                 ////    {
//                 ////        characters.Add(character);
//                 ////    }
//                 ////}
//
//                 uint memberID = (uint)packet.ReadInt();
//                 string displayName = packet.ReadString();
//                 short worldID = packet.ReadShort();
//                 bool inLobby = packet.ReadByte() == 1;
//                 ClanRank rank = (ClanRank)packet.ReadByte();
//
//                 members.Add(memberID, new ClanMember { MasterId = memberID, DisplayName = displayName, WorldId = worldID, InLobby = inLobby, Rank = rank });
//             }
//
//             byte bannedCount = packet.ReadByte();
//             for (int i = 0; i < bannedCount; i++)
//             {
//                 bannedMembers.Add((uint)packet.ReadInt(), packet.ReadString());
//             }
//
//             IClan clan = _clanManager.GetClanByName(clanName);
//             if (clan == null)
//             {
//                 clan = new Clan(clanName, members, bannedMembers);
//             }
//             else
//             {
//                 clan.SetMembers(members);
//                 clan.SetBannedMembers(bannedMembers);
//             }
//
//             foreach (ICharacter c in characters)
//             {
//                 c.Clan = clan;
//             }
//
//             _clanManager.PutClan(clan);
//         }
//     }
// }