using System.Collections.Generic;

namespace Raido.Server
{
    /// <summary>
    /// Options for a Raido hub.
    /// </summary>
    /// <typeparam name="THub">The type of the hub.</typeparam>
    public class RaidoHubOptions<THub> where THub : RaidoHub
    {
        internal List<IRaidoHubFilter>? HubFilters { get; set; }
    }
}
