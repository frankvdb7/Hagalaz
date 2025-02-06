// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     /// <summary>
//     ///     Attempts to request a find and leave the given clan chat channel.
//     /// </summary>
//     public class RemoveClanChatMemberRequestPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Composes a packet that leaves a friends chat channel.
//         /// </summary>
//         /// <param name="masterId">The server session key for the character leaving the chat channel.</param>
//         /// <param name="clanName">The name of the chat channel to leave.</param>
//         /// <param name="guestChannel">if set to <c>true</c> [guest channel].</param>
//         public RemoveClanChatMemberRequestPacketComposer(uint masterId, string clanName, bool guestChannel)
//         {
//             this.SetOpcode(1337);
//             SetType(SizeType.Short);
//
//             AppendInt((int)masterId);
//             AppendByte(guestChannel ? (byte)1 : (byte)0);
//             AppendString(clanName);
//         }
//     }
// }