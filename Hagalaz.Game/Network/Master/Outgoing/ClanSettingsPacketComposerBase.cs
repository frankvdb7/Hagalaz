// using Hagalaz.Game.Abstractions.Features.Clans;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Utilities;
//
// namespace Hagalaz.Game.Network.Master.Outgoing
// {
//     /// <summary>
//     ///
//     /// </summary>
//     public abstract class ClanSettingsPacketComposerBase : PacketComposer
//     {
//         /// <summary>
//         /// Initializes a new instance of the <see cref="ClanSettingsPacketComposerBase" /> class.
//         /// </summary>
//         /// <param name="clan">The clan.</param>
//         /// <param name="opcode">The opcode.</param>
//         public ClanSettingsPacketComposerBase(IClan clan, int opcode)
//         {
//             SetOpcode(opcode);
//             SetType(SizeType.Short);
//
//             AppendString(clan.Name);
//
//             AppendByte((byte)clan.Settings.RankToEnterCc);
//             AppendByte((byte)clan.Settings.RankToTalk);
//             AppendByte((byte)clan.Settings.RankToKick);
//             AppendShort(clan.Settings.TimeZone);
//             if (!string.IsNullOrEmpty(clan.Settings.Motto))
//             {
//                 AppendByte(1);
//                 AppendString(clan.Settings.Motto);
//             }
//             else
//                 AppendByte(0);
//             if (!string.IsNullOrEmpty(clan.Settings.ThreadID))
//             {
//                 AppendByte(1);
//                 AppendLong(clan.Settings.ThreadID.StringToLong());
//             }
//             else
//                 AppendByte(0);
//             AppendInt((clan.Settings.Recruiting ? 1 : 0)
//                                 | (clan.Settings.ClanTime ? 1 : 0) << 1
//                                 | clan.Settings.WorldID << 2
//                                 | clan.Settings.NationalFlag << 10);
//             AppendShort((short)(clan.Settings.MottifTop | (clan.Settings.MottifBottom << 8)));
//             AppendInt((ushort)clan.Settings.MottifColourLeftTop | (clan.Settings.MottifColourRightBottom << 16));
//             AppendInt((ushort)clan.Settings.PrimaryClanColour | (clan.Settings.SecondaryClanColour << 16));
//         }
//     }
// }
