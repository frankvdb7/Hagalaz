// using System.Threading.Tasks;
// using Hagalaz.Game.Abstractions.Features.Clans;
// using Hagalaz.Game.Features.Clans;
// using Hagalaz.Network.Common.Messages;
// using Hagalaz.Utilities;
//
// namespace Hagalaz.Game.Network.Master.Incoming
// {
//     /// <summary>
//     ///
//     /// </summary>
//     public abstract class ClanSettingsPacketHandlerBase
//     {
//         /// <summary>
//         /// Handles the specified type.
//         /// </summary>
//         /// <param name="t">The t.</param>
//         /// <param name="packet">The packet.</param>
//         /// <returns></returns>
//         public async Task HandleAsync(Packet packet)
//         {
//             var clanName = packet.ReadString();
//
//             var settings = new ClanSettings
//             {
//                 RankToEnterCc = (ClanRank)packet.ReadByte(),
//                 RankToTalk = (ClanRank)packet.ReadByte(),
//                 RankToKick = (ClanRank)packet.ReadByte(),
//                 TimeZone = packet.ReadShort()
//             };
//             if (packet.ReadByte() == 1)
//                 settings.Motto = packet.ReadString();
//             if (packet.ReadByte() == 1)
//                 settings.ThreadID = packet.ReadLong().LongToString();
//             int settings1 = packet.ReadInt();
//             settings.Recruiting = (settings1 & 0x1) == 1; // 1 bit
//             settings.ClanTime = ((settings1 >> 1) & 0x1) == 1; // 1 bit
//             settings.WorldID = (byte)((settings1 >> 2) & 0xFF); // 8 bits
//             settings.NationalFlag = (byte)(settings1 >> 10); // 8 bits
//             int mottif = packet.ReadShort();
//             settings.MottifTop = (byte)(mottif & 0xFF);
//             settings.MottifBottom = (byte)(mottif >> 8);
//             int colours1 = packet.ReadInt();
//             settings.MottifColourLeftTop = (short)(colours1 & 0xFFFF);
//             settings.MottifColourRightBottom = (short)(colours1 >> 16);
//             int colours2 = packet.ReadInt();
//             settings.PrimaryClanColour = (short)(colours2 & 0xFFFF);
//             settings.SecondaryClanColour = (short)(colours2 >> 16);
//
//             await OnSettingsRead(clanName, settings);
//         }
//
//         /// <summary>
//         /// Called when [handled].
//         /// </summary>
//         /// <param name="clanName">Name of the clan.</param>
//         /// <param name="settings">The settings.</param>
//         protected abstract Task OnSettingsRead(string clanName, IClanSettings settings);
//     }
// }
