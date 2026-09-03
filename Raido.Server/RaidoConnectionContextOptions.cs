using System;

namespace Raido.Server
{
    public class RaidoConnectionContextOptions
    {
        public TimeSpan KeepAliveInterval { get; init; }
        public TimeSpan ClientTimeoutInterval { get; init; }
        public bool StatefulReconnectEnabled { get; init; }
        public TimeSpan StatefulReconnectTimeout { get; init; }
    }
}
