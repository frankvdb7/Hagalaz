// using System.Threading.Tasks;
// using Hagalaz.Game.Abstractions.Features.FriendsChat;
// using Hagalaz.Game.Abstractions.Store;
// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Game.Network.Game.Outgoing;
// using Hagalaz.Game.Network.Model;
// using Hagalaz.Network.Common.Messages;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     public class NotifyFriendsChatMemberJoinPacketHandler : IMasterPacketHandler
//     {
//         private readonly ICharacterStore _characterStore;
//         public int Opcode => NotifyFriendsChatMemberJoin.Opcode;
//
//         public NotifyFriendsChatMemberJoinPacketHandler(ICharacterStore characterStore)
//         {
//             _characterStore = characterStore;
//         }
//
//         public async Task HandleAsync(Packet packet)
//         {
//             var chatName = packet.ReadString();
//             var newChatMember = new FriendsChatMemberDto()
//             {
//                 DisplayName = packet.ReadString(),
//                 PreviousDisplayName = packet.ReadByte() == 1 ? packet.ReadString() : null,
//                 WorldId = packet.ReadInt(),
//                 Rank = (FriendsChatRank)packet.ReadByte(),
//                 InLobby = (SessionType)packet.ReadByte() == SessionType.Lobby
//             };
//
//             var packetData = new AddFriendsChatMemberPacketComposer(newChatMember);
//
//             //await foreach (var member in _characterStore.FindAllAsync()
//             //    .Where(c => c.ChatChannels.FriendsChatName != null && c.ChatChannels.FriendsChatName.Equals(chatName, StringComparison.OrdinalIgnoreCase)))
//             //{
//             //    //await member.Session.SendPacketAsync(packetData);
//             //}
//         }
//     }
// }