// using System.Threading.Tasks;
// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Game.Abstractions.Store;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     /// <summary>
//     ///     Handler for friends chat join channel response.
//     /// </summary>
//     public class AddFriendsChatMemberResponsePacketHandler : IMasterPacketHandler
//     {
//         /// <summary>
//         ///     The character repository
//         /// </summary>
//         private readonly ICharacterStore _characterStore;
//
//         private readonly IMasterConnectionAdapter _connectionAdapter;
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="AddFriendsChatMemberResponsePacketHandler" /> class.
//         /// </summary>
//         /// <param name="characterStore">The character repository.</param>
//         /// <param name="connectionAdapter"></param>
//         public AddFriendsChatMemberResponsePacketHandler(ICharacterStore characterStore, IMasterConnectionAdapter connectionAdapter)
//         {
//             _characterStore = characterStore;
//             _connectionAdapter = connectionAdapter;
//         }
//
//         /// <summary>
//         ///     Gets the opcode.
//         /// </summary>
//         /// <value>
//         ///     The opcode.
//         /// </value>
//         public int Opcode => AddFriendsChatMemberResponse.Opcode;
//
//         /// <summary>
//         ///     Handles the packet.
//         /// </summary>
//         /// <param name="packet">The packet containing handle data.</param>
//         public async Task HandleAsync(Packet packet)
//         {
//             long key = packet.ReadLong();
//             //var character = await _characterStore.FindAllAsync().SingleOrDefaultAsync(c => c.Session.Id == key);
//
//             //if (character != null)
//             //{
//             //    AddFriendsChatMemberResponse.ResponseCode responseCode = (AddFriendsChatMemberResponse.ResponseCode)packet.ReadByte();
//
//             //    switch (responseCode)
//             //    {
//             //        case AddFriendsChatMemberResponse.ResponseCode.Success:
//             //        {
//             //            var channelName = packet.ReadString();
//             //            character.ChatChannels.OnFriendsChatJoined(channelName);
//             //            await _connectionAdapter.SendPacketAsync(new GetFriendsChatRequestPacketComposer(character.Session.Id));
//             //            //await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.FriendsChatWhiteText2, "Now talking in friends chat channel " + channelName + "."));
//             //            break;
//             //        }
//             //        case AddFriendsChatMemberResponse.ResponseCode.NotFound:
//             //            //await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.FriendsChatWhiteText2, "The channel you tried to join does not exist."));
//             //            break;
//             //        case AddFriendsChatMemberResponse.ResponseCode.Full:
//             //            //await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.FriendsChatWhiteText2, "The channel you tried to join is currently full."));
//             //            break;
//             //        case AddFriendsChatMemberResponse.ResponseCode.NotAllowed:
//             //            //await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.FriendsChatWhiteText2, "You are not allowed to join this user's friends chat channel."));
//             //            break;
//             //        case AddFriendsChatMemberResponse.ResponseCode.Unauthorized:
//             //            //await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.FriendsChatWhiteText2, "You do not have a high enough rank to join this friends chat channel."));
//             //            break;
//             //        case AddFriendsChatMemberResponse.ResponseCode.Banned:
//             //            //await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.FriendsChatWhiteText2, "You are temporarily banned from this friends chat channel."));
//             //            break;
//             //        case AddFriendsChatMemberResponse.ResponseCode.Failed:
//             //            //await character.Session.SendPacketAsync(new MessagePacketComposer(ClientMessageType.FriendsChatWhiteText2, "Error joining friends chat channel - please try again later!"));
//             //            break;
//             //    }
//             //}
//         }
//     }
// }