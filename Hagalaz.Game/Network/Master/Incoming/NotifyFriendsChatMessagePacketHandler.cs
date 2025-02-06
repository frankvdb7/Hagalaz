// using System;
// using System.Threading.Tasks;
// using Hagalaz.Game.Abstractions.Authorization;
// using Hagalaz.Game.Abstractions.Store;
// using Hagalaz.Game.Extensions;
// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Game.Network.Game.Outgoing;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Utilities;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     public class NotifyFriendsChatMessagePacketHandler : IMasterPacketHandler
//     {
//         private readonly ICharacterStore _characterStore;
//         public int Opcode => NotifyFriendsChatMessage.Opcode;
//
//         public NotifyFriendsChatMessagePacketHandler(ICharacterStore characterStore)
//         {
//             _characterStore = characterStore;
//         }
//
//         public async Task HandleAsync(Packet packet)
//         {
//             var chatName = packet.ReadString();
//             var displayName = packet.ReadString();
//             var previousDisplayName = packet.ReadString();
//
//             byte rights = 0;
//             var claims = packet.ReadShort();
//             for (var i = 0; i < claims; i++)
//             {
//                 var name = packet.ReadString();
//                 if (!Enum.TryParse<Permission>(name, out var permission))
//                 {
//                     continue;
//                 }
//
//                 var r = permission.ToClientRights();
//                 if (r > rights)
//                 {
//                     rights = r;
//                 }
//             }
//
//             var messageLength = packet.ReadShort();
//             var messagePayload = packet.GetRemainingData();
//             var messageId = SessionId.NewId();
//             var messagePacket = new AddFriendsChatMessagePacketComposer(displayName, previousDisplayName, chatName, rights, messageId, messageLength, messagePayload);
//
//             //await foreach (var member in _characterStore.FindAllAsync()
//             //    .Where(c => c.ChatChannels.FriendsChatName != null && c.ChatChannels.FriendsChatName.Equals(chatName, StringComparison.OrdinalIgnoreCase)))
//             //{
//             //    //await member.Session.SendPacketAsync(messagePacket);
//             //}
//         }
//     }
// }