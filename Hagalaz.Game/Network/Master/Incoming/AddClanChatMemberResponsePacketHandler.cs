// using System.Threading.Tasks;
// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Game.Abstractions.Store;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     /// <summary>
//     ///     Handler for clan chat join channel response.
//     /// </summary>
//     public class AddClanChatMemberResponsePacketHandler : IMasterPacketHandler
//     {
//         /// <summary>
//         ///     The character repository
//         /// </summary>
//         private readonly ICharacterStore _characterStore;
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="AddClanChatMemberResponsePacketHandler" /> class.
//         /// </summary>
//         /// <param name="characterStore">The character repository.</param>
//         public AddClanChatMemberResponsePacketHandler(ICharacterStore characterStore) => _characterStore = characterStore;
//
//         /// <summary>
//         ///     Gets the opcode.
//         /// </summary>
//         /// <value>
//         ///     The opcode.
//         /// </value>
//         public int Opcode => AddClanChatMemberResponse.Opcode;
//
//         /// <summary>
//         ///     Handles the packet.
//         /// </summary>
//         /// <param name="packet">The packet containing handle data.</param>
//         public async Task HandleAsync(Packet packet)
//         {
//             //var character = await _characterStore.FindAllAsync().Where(c => c.Session.Id == packet.ReadLong()).SingleOrDefaultAsync();
//
//             //if (character != null)
//             //{
//                 /*AddFriendsChatMemberResponseCode responseCode = (AddFriendsChatMemberResponseCode)packet.ReadByte();
//                 bool guestChannel = packet.ReadByte() == 1;
//
//                 switch (responseCode)
//                 {
//                     case AddFriendsChatMemberResponseCode.Success:
//                     {
//                         string clanName = packet.ReadString();
//                         if (guestChannel)
//                         {
//                             character.ChatChannels.OnGuestCcChannelJoined(clanName);
//                             await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ClanChatWhiteText, "You are now talking in guest clan chat channel " + clanName + "."));
//                         }
//                         else
//                         {
//                             character.ChatChannels.OnCcChannelJoined(clanName);
//                             await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ClanChatWhiteText, "You are now talking in clan chat channel " + clanName + "."));
//                         }
//
//                         break;
//                     }
//                     case AddFriendsChatMemberResponseCode.NotFound:
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ClanChatWhiteText, "The channel you tried to join does not exist."));
//                         break;
//                     case AddFriendsChatMemberResponseCode.Full:
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ClanChatWhiteText, "The channel you tried to join is currently full."));
//                         break;
//                     case AddFriendsChatMemberResponseCode.NotAllowed:
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ClanChatWhiteText, "You are not allowed to join this clan's chat channel."));
//                         break;
//                     case AddFriendsChatMemberResponseCode.Unauthorized:
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ClanChatWhiteText, "You do not have a high enough rank to join this clan chat channel."));
//                         break;
//                     case AddFriendsChatMemberResponseCode.Banned:
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ClanChatWhiteText, "You are banned from this clan chat channel."));
//                         break;
//                     case AddFriendsChatMemberResponseCode.Failed:
//                         await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.ClanChatWhiteText, "Error joining clan chat channel - please try again later!"));
//                         break;
//                 }*/
//             //}
//         }
//     }
// }