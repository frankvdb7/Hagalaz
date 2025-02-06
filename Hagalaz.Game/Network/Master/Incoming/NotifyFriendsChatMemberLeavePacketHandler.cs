// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Hagalaz.Game.Abstractions.Store;
// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Game.Network.Game.Outgoing;
// using Hagalaz.Game.Network.Model;
// using Hagalaz.Network.Common.Messages;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     public class NotifyFriendsChatMemberLeavePacketHandler : IMasterPacketHandler
//     {
//         private readonly ICharacterStore _characterStore;
//         public int Opcode => NotifyFriendsChatMemberLeave.Opcode;
//
//         public NotifyFriendsChatMemberLeavePacketHandler(ICharacterStore characterStore)
//         {
//             _characterStore = characterStore;
//         }
//
//         public async Task HandleAsync(Packet packet)
//         {
//             var chatName = packet.ReadString();
//             var memberCount = packet.ReadByte();
//             var members = new List<FriendsChatMemberDto>(memberCount);
//             for (var i = 0; i < memberCount; i++)
//             {
//                 members.Add(new FriendsChatMemberDto()
//                 {
//                     DisplayName = packet.ReadString(), PreviousDisplayName = packet.ReadByte() == 1 ? packet.ReadString() : null, WorldId = packet.ReadInt()
//                 });
//             }
//
//             var packetData = members.Select(m => new RemoveFriendsChatMemberPacketComposer(m)).ToList();
//
//             //await foreach (var member in _characterStore.FindAllAsync()
//             //    .Where(c => c.ChatChannels.FriendsChatName != null && c.ChatChannels.FriendsChatName.Equals(chatName, StringComparison.OrdinalIgnoreCase)))
//             //{
//             //    foreach (var m in packetData)
//             //    {
//             //        //await member.Session.SendPacketAsync(m);
//             //    }
//             //}
//         }
//     }
// }