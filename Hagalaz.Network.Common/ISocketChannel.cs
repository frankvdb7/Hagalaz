using System;
using System.Threading.Tasks;

namespace Hagalaz.Network.Common
{
    /// <summary>
    /// Represents the method that will handle the event raised when data is received from a socket channel.
    /// </summary>
    /// <param name="data">The byte array containing the received data.</param>
    public delegate void DataRouteEventHandler(byte[] data);

    /// <summary>
    /// Represents the method that will handle the event raised when a socket channel is disconnected.
    /// </summary>
    public delegate void DisconnectEventHandler();

    /// <summary>
    /// Defines the contract for a communication channel over a socket, providing methods for sending and receiving data,
    /// and managing the connection state.
    /// </summary>
    public interface ISocketChannel : IDisposable
    {
        /// <summary>
        /// Gets the unique identifier for this connection.
        /// </summary>
        string ConnectionId { get; }

        /// <summary>
        /// Gets the IP address of the remote endpoint.
        /// </summary>
        string IpAddress { get; }

        /// <summary>
        /// Gets a value indicating whether the socket channel is currently connected.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// Starts the channel's data listening loop and associates the specified event handlers for data reception and disconnection.
        /// </summary>
        /// <param name="routeDataEvent">The event handler for incoming data.</param>
        /// <param name="disconnectEvent">The event handler for disconnection events.</param>
        void Start(DataRouteEventHandler routeDataEvent, DisconnectEventHandler disconnectEvent);

        /// <summary>
        /// Replaces the existing data and disconnection event handlers with new ones.
        /// </summary>
        /// <param name="routeDataEvent">The new event handler for incoming data.</param>
        /// <param name="disconnectEvent">The new event handler for disconnection events.</param>
        void SwitchEvents(DataRouteEventHandler routeDataEvent, DisconnectEventHandler disconnectEvent);

        /// <summary>
        /// Asynchronously sends a byte array over the socket channel.
        /// </summary>
        /// <param name="data">The data to be sent.</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous send operation.</returns>
        Task SendDataAsync(byte[] data);

        /// <summary>
        /// Closes the connection and disconnects the channel.
        /// </summary>
        void Disconnect();
    }
}
