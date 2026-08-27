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

        /// <summary>
        /// Gets or sets a value indicating whether the connection can rebind to a replacement physical transport.
        /// </summary>
        public bool StatefulReconnectEnabled { get; init; }

        /// <summary>
        /// Gets or sets the maximum time to wait for a replacement physical transport.
        /// </summary>
        public TimeSpan StatefulReconnectTimeout { get; init; }
    }
}
