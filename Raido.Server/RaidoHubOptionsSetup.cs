using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Raido.Server
{
    public class RaidoHubOptionsSetup<THub> : IConfigureOptions<RaidoHubOptions<THub>> where THub : RaidoHub
    {
        private readonly RaidoOptions _raidoOptions;

        public RaidoHubOptionsSetup(IOptions<RaidoOptions> options)
        {
            _raidoOptions = options.Value;
        }

        public void Configure(RaidoHubOptions<THub> options)
        {
            if (_raidoOptions.GlobalHubFilters != null)
            {
                options.HubFilters = new List<IRaidoHubFilter>(_raidoOptions.GlobalHubFilters);
            }
        }
    }
}
