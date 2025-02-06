using System;
using System.Collections.Generic;

namespace Raido.Server
{
    /// <summary>
    /// Options used to configure kestrel instances.
    /// </summary>
    public class RaidoOptions
    {
        /// <summary>
        /// Gets or sets the interval used by the server to send keep alive pings to connected clients. The default interval is 15 seconds.
        /// </summary>
        public TimeSpan? KeepAliveInterval { get; set; }

        /// <summary>
        /// Gets or sets the time window clients have to send a message before the server closes the connection. The default timeout is 30 seconds.
        /// </summary>
        public TimeSpan? ClientTimeoutInterval { get; set; }
        
        /// <summary>
        /// Gets or sets the maximum message size of a single incoming hub message. The default is 32KB.
        /// </summary>
        public long? MaximumReceiveMessageSize { get; set; }

        internal List<IRaidoHubFilter>? GlobalHubFilters { get; set; }
    }
}