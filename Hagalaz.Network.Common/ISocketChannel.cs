using System;
using System.Threading.Tasks;

namespace Hagalaz.Network.Common
{
    /// <summary>
    /// The method that will handle the event raised when data is received via the TCP listener.
    /// </summary>
    /// <param name="data">The data recieved and to be handled.</param>
    public delegate void DataRouteEventHandler(byte[] data);
    /// <summary>
    /// The method that will handle the event raised when the client errors or disconnects.
    /// </summary>
    public delegate void DisconnectEventHandler();

    /// <summary>
    /// 
    /// </summary>
    public interface ISocketChannel : IDisposable
    {
        /// <summary>
        /// Gets the connection identifier.
        /// </summary>
        /// <value>
        /// The connection identifier.
        /// </value>
        string ConnectionId { get; }
        /// <summary>
        /// Gets the ip address.
        /// </summary>
        /// <value>
        /// The ip address.
        /// </value>
        string IpAddress { get; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="ISocketChannel"/> is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if connected; otherwise, <c>false</c>.
        /// </value>
        bool Connected { get; }
        /// <summary>
        /// Starts the specified route data event.
        /// </summary>
        /// <param name="routeDataEvent">The route data event.</param>
        /// <param name="disconnectEvent">The disconnect event.</param>
        void Start(DataRouteEventHandler routeDataEvent, DisconnectEventHandler disconnectEvent);
        /// <summary>
        /// Switches the events.
        /// </summary>
        /// <param name="routeDataEvent">The route data event.</param>
        /// <param name="disconnectEvent">The disconnect event.</param>
        void SwitchEvents(DataRouteEventHandler routeDataEvent, DisconnectEventHandler disconnectEvent);
        /// <summary>
        /// Sends the data asynchronous.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        Task SendDataAsync(byte[] data);
        /// <summary>
        /// Disconnects this instance.
        /// </summary>
        void Disconnect();
    }
}
