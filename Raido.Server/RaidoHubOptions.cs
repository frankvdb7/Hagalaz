using System.Collections.Generic;

namespace Raido.Server
{
    public class RaidoHubOptions<THub> where THub : RaidoHub
    {
        internal List<IRaidoHubFilter>? HubFilters { get; set; }
    }
}
