// using System.Threading.Tasks;
// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Game.Abstractions.Store;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     /// <summary>
//     ///     Packet for handling a member being kicked.
//     /// </summary>
//     public class RemoveFriendsChatMemberResponsePacketHandler : IMasterPacketHandler
//     {
//         private readonly ICharacterStore _characterStore;
//
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="RemoveFriendsChatMemberResponsePacketHandler" /> class.
//         /// </summary>
//         /// <param name="characterStore">The character repository.</param>
//         public RemoveFriendsChatMemberResponsePacketHandler(ICharacterStore characterStore) => _characterStore = characterStore;
//
//         /// <summary>
//         ///     Gets the opcode.
//         /// </summary>
//         /// <value>
//         ///     The opcode.
//         /// </value>
//         public int Opcode => RemoveFriendsChatMemberResponse.Opcode;
//
//         /// <summary>
//         ///     Handles the packet.
//         /// </summary>
//         /// <param name="packet">The packet containing handle data.</param>
//         public async Task HandleAsync(Packet packet)
//         {
//             //long sessionId = packet.ReadLong();
//             //var character = await _characterStore.FindAllAsync().SingleOrDefaultAsync(c => c.Session.Id == sessionId);
//
//             //if (character != null)
//             //{
//             //    if (character.ChatChannels.InFriendsChat)
//             //    {
//             //        character.ChatChannels.OnFcChannelLeft();
//             //    }
//
//             //    await character.Session.SendPacketAsync(new AddFriendsChatMembersPacketComposer());
//
//             //    var succeeded = packet.ReadByte() == 1;
//             //}
//         }
//     }
// }