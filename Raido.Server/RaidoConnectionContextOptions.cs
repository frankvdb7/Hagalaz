using System;

namespace Raido.Server
{
    public class RaidoConnectionContextOptions
    {
        /// <summary>
        /// Gets or sets the interval used to send keep alive pings to connected clients.
        /// </summary>
        public TimeSpan KeepAliveInterval { get; init; }

        /// <summary>
        /// Gets or sets the time window clients have to send a message before the server closes the connection.
        /// </summary>
        public TimeSpan ClientTimeoutInterval { get; init; }
    }
}