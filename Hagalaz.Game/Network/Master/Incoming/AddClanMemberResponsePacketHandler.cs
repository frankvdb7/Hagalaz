// using System.Threading.Tasks;
// using Hagalaz.Game.Abstractions.Features.Clans;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Game.Abstractions.Store;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     /// <summary>
//     ///     Handler for join response clan packet.
//     /// </summary>
//     public class AddClanMemberResponsePacketHandler : IMasterPacketHandler
//     {
//         /// <summary>
//         /// </summary>
//         private readonly ICharacterStore _characterStore;
//
//         private readonly IClanService _clanManager;
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="AddClanMemberResponsePacketHandler" /> class.
//         /// </summary>
//         /// <param name="characterStore">The character repository.</param>
//         /// <param name="clanManager"></param>
//         public AddClanMemberResponsePacketHandler(ICharacterStore characterStore, IClanService clanManager)
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
//         public int Opcode => 124223124;
//
//         /// <summary>
//         ///     Handles the packet.
//         /// </summary>
//         /// <param name="packet">The packet containing handle data.</param>
//         public async Task HandleAsync(Packet packet)
//         {
//             long key = packet.ReadLong();
//             //var character = await _characterStore.FindAllAsync().Where(c => c.Session.Id == key).SingleOrDefaultAsync();
//
//             //if (character != null)
//             //{
//                 /*AddFriendsChatMemberResponseCode responseCode = (AddFriendsChatMemberResponseCode)packet.ReadByte();
//
//                 switch (responseCode)
//                 {
//                     case AddFriendsChatMemberResponseCode.Success:
//                     {
//                         string clanName = packet.ReadString();
//                         character.Clan = _clanManager.GetClanByName(clanName);
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ChatboxText, $"You are now talking in {clanName}."));
//                         break;
//                     }
//                     case AddFriendsChatMemberResponseCode.NotFound:
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ChatboxText, "The clan you tried to join does not exist."));
//                         break;
//                     case AddFriendsChatMemberResponseCode.Full:
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ChatboxText, "The clan you tried to join is currently full."));
//                         break;
//                     case AddFriendsChatMemberResponseCode.NotAllowed:
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ChatboxText, "You are not allowed to join this clan."));
//                         break;
//                     case AddFriendsChatMemberResponseCode.Unauthorized:
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ChatboxText, "You do not have a high enough rank to join this clan."));
//                         break;
//                     case AddFriendsChatMemberResponseCode.Banned:
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ChatboxText, "You are banned from this clan."));
//                         break;
//                     case AddFriendsChatMemberResponseCode.Failed:
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ChatboxText, "Error joining clan - please try again later!"));
//                         break;
//                 }*/
//             //}
//         }
//     }
// }