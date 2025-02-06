// using System.Threading.Tasks;
// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Game.Abstractions.Store;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     /// <summary>
//     ///     Handles a given list of friends chat members, and sends it to the client.
//     /// </summary>
//     public class GetFriendsChatResponsePacketHandler : IMasterPacketHandler
//     {
//         /// <summary>
//         ///     The character repository
//         /// </summary>
//         private readonly ICharacterStore _characterStore;
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="GetFriendsChatResponsePacketHandler" /> class.
//         /// </summary>
//         /// <param name="characterStore">The character repository.</param>
//         public GetFriendsChatResponsePacketHandler(ICharacterStore characterStore) => _characterStore = characterStore;
//
//         /// <summary>
//         ///     Gets the opcode.
//         /// </summary>
//         /// <value>
//         ///     The opcode.
//         /// </value>
//         public int Opcode => GetFriendsChatResponse.Opcode;
//
//         /// <summary>
//         ///     Handles the packet.
//         /// </summary>
//         /// <param name="packet">The packet containing handle data.</param>
//         public async Task HandleAsync(Packet packet)
//         {
//             //var sessionId = packet.ReadLong();
//             //var responseCode = (GetFriendsChatResponse.ResponseCode)packet.ReadByte();
//             //var character = await _characterStore.FindAllAsync().FirstOrDefaultAsync(c => c.Session.Id == sessionId);
//             //if (character == null)
//             //{
//             //    return;
//             //}
//
//             //if (responseCode == GetFriendsChatResponse.ResponseCode.NotFound)
//             //{
//             //    character.SendChatMessage("Friends chat not found");
//             //    return;
//             //}
//
//             //var channelName = packet.ReadString();
//             //var ownerDisplayName = packet.ReadString();
//             //var rankToKick = (FriendsChatRank)packet.ReadByte();
//             //var ownerPreviousDisplayName = packet.ReadString();
//             //var memberCount = packet.ReadInt();
//             //var members = new List<FriendsChatMemberDto>(memberCount);
//             //for (var i = 0; i < memberCount; i++)
//             //{
//             //    members.Add(new FriendsChatMemberDto()
//             //    {
//             //        DisplayName = packet.ReadString(),
//             //        PreviousDisplayName = packet.ReadString(),
//             //        Rank = (FriendsChatRank)packet.ReadByte(),
//             //        WorldId = packet.ReadInt(),
//             //        InLobby = (SessionType)packet.ReadByte() == SessionType.Lobby
//             //    });
//             //}
//
//             //await character.Session.SendPacketAsync(new AddFriendsChatMembersPacketComposer(ownerDisplayName, ownerPreviousDisplayName, channelName,
//             //    rankToKick, members));
//         }
//     }
// }