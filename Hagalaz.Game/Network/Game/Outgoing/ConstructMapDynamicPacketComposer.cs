// using System.Threading.Tasks;
// using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
// using Hagalaz.Network.Common.Messages;
// using PacketComposer = Hagalaz.Network.Common.Messages.PacketComposer;
//
// namespace Hagalaz.Game.Network.Game.Outgoing
// {
//     /// <summary>
//     ///     Class ConstructMapDynamicPacketComposer
//     /// </summary>
//     public class ConstructMapDynamicPacketComposer : PacketComposer
//     {
//         /// <summary>
//         ///     Constructs packet used for loading regions.
//         /// </summary>
//         public ConstructMapDynamicPacketComposer() => SetType(SizeType.Short);
//
//         /// <summary>
//         ///     Write's region data.
//         /// </summary>
//         /// <param name="composer">Composer to write for.</param>
//         /// <param name="character">Character to write for.</param>
//         /// <param name="forceRefresh">if set to <c>true</c> [force refresh].</param>
//         public static Task<PacketComposer> WriteDataAsync(PacketComposer composer, ICharacter character, bool forceRefresh)
//         {
//             return Task.FromResult(composer);
//             // var regionManager = ServiceLocator.Current.GetInstance<IMapRegionService>();
//             //
//             // composer.SetOpcode(153);
//             // composer.AppendByteC(1); // map load type.
//             // composer.AppendLeShortA((short)character.Viewport.ViewLocation.RegionPartY);
//             // composer.AppendByteS(forceRefresh ? (byte)1 : (byte)0);
//             // composer.AppendByte((byte)character.Viewport.MapSize.Type);
//             // composer.AppendShortA((short)character.Viewport.ViewLocation.RegionPartX);
//             //
//             // var viewPortSize = character.Viewport.MapSize.Size >> 4; // equals / 16
//             // var regionPartX = character.Location.RegionPartX;
//             // var regionPartY = character.Location.RegionPartY;
//             //
//             // var xteaRequests = new LinkedList<int>();
//             // composer.InitializeBit();
//             // for (var z = 0; z < 4; z++)
//             // {
//             //     for (var regPartX = regionPartX - viewPortSize; regPartX <= regionPartX + viewPortSize; regPartX++)
//             //     {
//             //         for (var regPartY = regionPartY - viewPortSize; regPartY <= regionPartY + viewPortSize; regPartY++)
//             //         {
//             //             int regionID = ((regPartX / 8) << 8) + regPartY / 8;
//             //             var region = regionManager.GetOrCreateMapRegion(regionID, character.Location.Dimension, true);
//             //             var data = region.GetRegionPartData(regPartX, regPartY, z);
//             //             int hash = data.GetHashCode();
//             //             composer.AppendBits(1, hash != 0 ? 1 : 0);
//             //             if (hash != 0)
//             //             {
//             //                 composer.AppendBits(26, hash);
//             //                 // Now to request XTEA.
//             //                 int drawRegionID = ((data.DrawRegionPartX / 8) << 8) + data.DrawRegionPartY / 8;
//             //                 if (!xteaRequests.Contains(drawRegionID))
//             //                 {
//             //                     xteaRequests.AddLast(drawRegionID);
//             //                 }
//             //             }
//             //         }
//             //     }
//             // }
//             //
//             // composer.FinishBit();
//             //
//             // foreach (var xtea in xteaRequests.Select(regionID => regionManager.GetXtea(regionID)))
//             // {
//             //     for (int i = 0; i < 4; i++)
//             //     {
//             //         composer.AppendInt(xtea[i]);
//             //     }
//             // }
//             //
//             // return Task.FromResult(composer);
//         }
//     }
// }