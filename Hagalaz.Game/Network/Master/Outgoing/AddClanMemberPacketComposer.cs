// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     /// <summary>
//     ///     Attempts to request a find and join for the given clan.
//     /// </summary>
//     public class AddClanMemberPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Composes a packet that finds and joins a clan chat channel.
//         /// </summary>
//         /// <param name="masterId">The server session key for the character looking for the chat channel.</param>
//         /// <param name="clanName">Name of the clan.</param>
//         public AddClanMemberPacketComposer(uint masterId, string clanName)
//         {
//             this.SetOpcode(1337);
//             SetType(SizeType.Short);
//
//             AppendInt((int)masterId);
//             AppendString(clanName);
//         }
//     }
// }