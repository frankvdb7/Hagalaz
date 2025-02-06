using System.Collections.Generic;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.JagGrab.Network
{
    /// <summary>
    /// Handles JAGGRAB fileserver connection requests.
    /// </summary>
    /// <seealso cref="ConnectionAdapterBase" />
    public class ConnectionHandler
    {
        #region Fields
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger<ConnectionHandler> _logger;
        /// <summary>
        /// Gets the list of current connections.
        /// </summary>
        //public List<ISocketChannel> CurrentConnections { get; }
        #endregion Fields

        #region Constructors
        /// <summary>
        /// Constructs a connection handler for JAGGRAB requests.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public ConnectionHandler(IOptions<JagGrabConfig> options, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ConnectionHandler>();
            //CurrentConnections = new List<ISocketChannel>();
        }
        #endregion Constructors

        #region Methods
        /// <summary>
        /// A connection is opened for a JAGGRAB request.
        /// </summary>
        /// <param name="channel">The client created.</param>
        //public void ConnectionOpened(ISocketChannel channel)
        //{
           // _logger.LogDebug("JAGGRAB connection opened [id=" + channel.ConnectionId + ", ip=" + channel.IpAddress + "].");
            //lock (CurrentConnections)
           // {
           //     CurrentConnections.Add(channel);
           // }
            //new UpdateSession(client);
        //}


        /// <summary>
        /// A connection is dropped.
        /// </summary>
        /// <param name="channel">The client to drop.</param>
        //public void ConnectionClosed(ISocketChannel channel)
        //{
        //    lock (CurrentConnections)
        //    {
        //        if (channel != null && CurrentConnections.Contains(channel))
        //        {
        //            channel.Dispose();
        //            CurrentConnections.Remove(channel);
        //            _logger.LogDebug("JAGGRAB Connection dropped [id=" + channel.ConnectionId + "].");
        //        }
        //    }
        //}

        /// <summary>
        /// A connection attempt was denied.
        /// </summary>
        /// <param name="socket">The socket attached to the connection.</param>
        public void ConnectionDenied(Socket socket) => _logger.LogInformation("A JAGGRAB connection was denied access [ip=" + socket.RemoteEndPoint + "].");

        #endregion Methods
    }
}
