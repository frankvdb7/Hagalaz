// using System.Linq;
// using Hagalaz.Game.Abstractions.Features.Clans;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Utilities;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Game.Outgoing
// {
//     /// <summary>
//     /// </summary>
//     /// <seealso cref="PacketComposer" />
//     public class ClanPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Initializes a new instance of the <see cref="ClanPacketComposer" /> class.
//         /// </summary>
//         /// <param name="clan">The clan.</param>
//         /// <param name="mainClan">if set to <c>true</c> [my clan].</param>
//         public ClanPacketComposer(IClan clan, bool mainClan)
//         {
//             byte someVersion = 3;
//
//             SetOpcode(1);
//             SetType(SizeType.Short);
//
//             AppendByte(mainClan ? (byte)1 : (byte)0);
//
//             AppendByte(someVersion); // clan version
//             AppendByte(0x2); // read name as string, 0x1 as long
//             AppendInt(0); // update number?
//             AppendInt(0); // dunno
//             AppendShort((short)clan.Members.Count());
//             AppendByte((byte)clan.BannedMembers.Count());
//             AppendString(clan.Name);
//             if (someVersion >= 4)
//             {
//                 AppendInt(0); // not used
//             }
//
//             AppendByte(clan.Settings.RankToEnterCc == ClanRank.Guest ? (byte)1 : (byte)0);
//             AppendByte(1); // unknown - something with clan citadels
//             AppendByte(0); // some rank for something in clan channel
//             AppendByte(0); // unknown
//             AppendByte(0); // unknown
//             foreach (IClanMember member in clan.Members)
//             {
//                 AppendString(member.DisplayName);
//                 AppendByte((byte)member.Rank);
//                 if (someVersion >= 2)
//                 {
//                     AppendInt(0); // unknown
//                 }
//
//                 if (someVersion >= 5)
//                 {
//                     AppendShort(0); // unknown
//                 }
//             }
//
//             foreach (string banned in clan.BannedMembers.Values)
//             {
//                 AppendString(banned);
//             }
//
//             if (someVersion >= 3)
//             {
//                 short count = 0;
//                 if (clan.Settings.TimeZone != -1)
//                 {
//                     count++;
//                 }
//
//                 if (!string.IsNullOrEmpty(clan.Settings.Motto))
//                 {
//                     count++;
//                 }
//
//                 if (!string.IsNullOrEmpty(clan.Settings.ThreadID))
//                 {
//                     count++;
//                 }
//
//                 if (clan.Settings.Recruiting || clan.Settings.ClanTime ||
//                     clan.Settings.WorldID != 0 || clan.Settings.NationalFlag != 0)
//                 {
//                     count++;
//                 }
//
//                 if (clan.Settings.MottifTop != 0 || clan.Settings.MottifBottom != 0)
//                 {
//                     count++;
//                 }
//
//                 if (clan.Settings.MottifColourLeftTop != -1)
//                 {
//                     count++;
//                 }
//
//                 if (clan.Settings.MottifColourRightBottom != -1)
//                 {
//                     count++;
//                 }
//
//                 if (clan.Settings.PrimaryClanColour != -1)
//                 {
//                     count++;
//                 }
//
//                 if (clan.Settings.SecondaryClanColour != -1)
//                 {
//                     count++;
//                 }
//
//                 AppendShort(count);
//                 if (clan.Settings.TimeZone != -1)
//                 {
//                     AppendInt(0 | (0 << 30));
//                     int time = (clan.Settings.TimeZone - 72) / 3 * 30;
//                     AppendInt(time); // 30 minutes per increment
//                 }
//
//                 if (!string.IsNullOrEmpty(clan.Settings.Motto))
//                 {
//                     AppendInt(1 | (2 << 30));
//                     AppendString(clan.Settings.Motto);
//                 }
//
//                 if (!string.IsNullOrEmpty(clan.Settings.ThreadID))
//                 {
//                     AppendInt(2 | (1 << 30));
//                     AppendLong(clan.Settings.ThreadID.StringToLong());
//                 }
//
//                 if (clan.Settings.Recruiting || clan.Settings.ClanTime ||
//                     clan.Settings.WorldID != 0 || clan.Settings.NationalFlag != 0)
//                 {
//                     AppendInt(3 | (0 << 30));
//                     AppendInt((clan.Settings.Recruiting ? 1 : 0)
//                               | ((clan.Settings.ClanTime ? 1 : 0) << 1)
//                               | (clan.Settings.WorldID << 2)
//                               | (clan.Settings.NationalFlag << 10));
//                 }
//
//                 if (clan.Settings.MottifTop != 0 || clan.Settings.MottifBottom != 0)
//                 {
//                     AppendInt(13 | (0 << 30));
//                     AppendInt((clan.Settings.MottifTop + 1) | ((clan.Settings.MottifBottom + 1) << 16));
//                 }
//
//                 if (clan.Settings.MottifColourLeftTop != -1)
//                 {
//                     AppendInt(16 | (0 << 30));
//                     AppendInt(clan.Settings.MottifColourLeftTop);
//                 }
//
//                 if (clan.Settings.MottifColourRightBottom != -1)
//                 {
//                     AppendInt(17 | (0 << 30));
//                     AppendInt(clan.Settings.MottifColourRightBottom);
//                 }
//
//                 if (clan.Settings.PrimaryClanColour != -1)
//                 {
//                     AppendInt(18 | (0 << 30));
//                     AppendInt(clan.Settings.PrimaryClanColour);
//                 }
//
//                 if (clan.Settings.SecondaryClanColour != -1)
//                 {
//                     AppendInt(19 | (0 << 30));
//                     AppendInt(clan.Settings.SecondaryClanColour);
//                 }
//             }
//         }
//     }
// }