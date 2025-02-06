// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     /// <summary>
//     ///     Attempts to request a find and leave the given clan.
//     /// </summary>
//     public class RemoveClanMemberRequestPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Composes a packet that leaves a friends chat channel.
//         /// </summary>
//         /// <param name="masterId">The server session key for the character leaving the chat channel.</param>
//         /// <param name="clanName">The name of the chat channel to leave.</param>
//         public RemoveClanMemberRequestPacketComposer(uint masterId, string clanName)
//         {
//             this.SetOpcode(1337);
//             SetType(SizeType.Short);
//
//             AppendInt((int)masterId);
//             AppendString(clanName);
//         }
//     }
// }