// using Hagalaz.Game.Messages.Model;
// using Hagalaz.Game.Network.Model;
// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     /// <summary>
//     ///     A packet composer used to notify any friends chat settings changes.
//     /// </summary>
//     public class SetFriendsChatSettingsRequestPacketComposer : PacketComposer
//     {
//         public SetFriendsChatSettingsRequestPacketComposer(long sessionId, FriendsChatSettingsDto settingsDto)
//         {
//             SetOpcode(SetFriendsChatSettingsRequest.Opcode);
//             SetType(SizeType.Short);
//
//             AppendLong(sessionId);
//             if (string.IsNullOrEmpty(settingsDto.ChatName))
//             {
//                 AppendByte(0);
//             }
//             else
//             {
//                 AppendString(settingsDto.ChatName);
//             }
//
//             AppendByte(settingsDto.LootShareEnabled == null ? (byte)0 : (byte)1);
//         }
//     }
// }