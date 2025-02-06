// using System.Threading.Tasks;
// using Hagalaz.Game.Abstractions.Store;
// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Network.Common.Messages;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     public class AddFriendsChatMessageResponsePacketHandler : IMasterPacketHandler
//     {
//         private readonly ICharacterStore _characterStore;
//         public int Opcode => AddFriendsChatMessageResponse.Opcode;
//
//         public AddFriendsChatMessageResponsePacketHandler(ICharacterStore characterStore)
//         {
//             _characterStore = characterStore;
//         }
//
//         public async Task HandleAsync(Packet packet)
//         {
//             //var sessionId = packet.ReadLong();
//             //var response = (AddFriendsChatMessageResponse.ResponseCode)packet.ReadByte();
//
//             //var character = await _characterStore.FindAllAsync().SingleOrDefaultAsync(c => c.Session.Id == sessionId);
//
//             //if (character != null)
//             //{
//             //    if (response == AddFriendsChatMessageResponse.ResponseCode.NotAllowed)
//             //    {
//             //        character.SendChatMessage("You do not have a high enough rank to talk in this friends chat channel.", ChatMessageType.FriendsChatWhiteText2);
//             //    }
//             //}
//         }
//     }
// }