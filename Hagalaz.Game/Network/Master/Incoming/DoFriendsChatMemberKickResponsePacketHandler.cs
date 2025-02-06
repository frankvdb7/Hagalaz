// using System.Threading.Tasks;
// using Hagalaz.Game.Abstractions.Store;
// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Network.Common.Messages;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     public class DoFriendsChatMemberKickResponsePacketHandler : IMasterPacketHandler
//     {
//         private readonly ICharacterStore _characterStore;
//         public int Opcode => DoFriendsChatMemberKickResponse.Opcode;
//
//         public DoFriendsChatMemberKickResponsePacketHandler(ICharacterStore characterStore)
//         {
//             _characterStore = characterStore;
//         }
//
//         public async Task HandleAsync(Packet packet)
//         {
//             var sessionId = packet.ReadLong();
//             //var character = await _characterStore.FindAllAsync().Where(c => c.Session.Id == sessionId).SingleOrDefaultAsync();
//             //if (character == null)
//             //{
//             //    return;
//             //}
//
//             //var responseCode = (DoFriendsChatMemberKickResponse.ResponseCode)packet.ReadByte();
//             //switch (responseCode)
//             //{
//             //    case DoFriendsChatMemberKickResponse.ResponseCode.LowRank: character.SendChatMessage("You do not have a high enough rank to kick this member.", ChatMessageType.FriendsChatWhiteText2);
//             //        break;
//             //    case DoFriendsChatMemberKickResponse.ResponseCode.NotFound: character.SendChatMessage("The member you tried to kick does not exist.", ChatMessageType.FriendsChatWhiteText2);
//             //        break;
//             //    case DoFriendsChatMemberKickResponse.ResponseCode.Failed: character.SendChatMessage("Error kicking member - please try again later!", ChatMessageType.FriendsChatWhiteText2);
//             //        break;
//             //}
//         }
//     }
// }