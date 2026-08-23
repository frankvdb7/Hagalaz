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
        /// Gets whether this logical connection retains its lifetime during a physical transport loss.
        /// </summary>
        public bool StatefulReconnectEnabled { get; init; }

        /// <summary>
        /// Gets the bounded period in which a replacement physical transport may rebind.
        /// </summary>
        public TimeSpan StatefulReconnectGracePeriod { get; init; } = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Gets the time provider used for reconnect grace timing and connection timestamps.
        /// </summary>
        public TimeProvider TimeProvider { get; init; } = TimeProvider.System;
    }
}
