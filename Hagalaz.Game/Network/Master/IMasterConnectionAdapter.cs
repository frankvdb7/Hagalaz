// using System.Threading.Tasks;
// //using Hagalaz.Network.Common.Messages;
//
// namespace Hagalaz.Game.Network.Master
// {
//     /// <summary>
//     /// </summary>
//     public interface IMasterConnectionAdapter
//     {
//         /// <summary>
//         ///     Gets a value indicating whether this <see cref="IMasterConnectionAdapter" /> is connected.
//         /// </summary>
//         /// <value>
//         ///     <c>true</c> if connected; otherwise, <c>false</c>.
//         /// </value>
//         bool Connected { get; }
//
//         /// <summary>
//         ///     Gets the ip address.
//         /// </summary>
//         /// <value>
//         ///     The ip address.
//         /// </value>
//         string IpAddress { get; }
//
//         /// <summary>
//         ///     Sends the data asynchronous.
//         /// </summary>
//         /// <param name="data">The data.</param>
//         /// <returns></returns>
//         Task SendDataAsync(byte[] data);
//
//         /// <summary>
//         ///     Sends the packet asynchronous.
//         /// </summary>
//         /// <param name="composer">The composer.</param>
//         /// <returns></returns>
//         Task SendPacketAsync(PacketComposer composer);
//     }
// }