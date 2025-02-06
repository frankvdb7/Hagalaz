using System.Diagnostics;

namespace Raido.Server.Internal
{
    internal sealed class RaidoServerActivitySource
    {
        internal const string Name = "Raido.Server";
        internal const string DispatchMessage = $"{Name}.DispatchMessage";
        internal const string OnConnected = $"{Name}.OnConnected";
        internal const string OnDisconnected = $"{Name}.OnDisconnected";

        public ActivitySource ActivitySource { get; } = new ActivitySource(Name);
    }
}