using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Raido.Server
{
    /// <summary>
    /// Sets up the default options for a Raido hub.
    /// </summary>
    /// <typeparam name="THub">The type of the hub.</typeparam>
    public class RaidoHubOptionsSetup<THub> : IConfigureOptions<RaidoHubOptions<THub>> where THub : RaidoHub
    {
        private readonly RaidoOptions _raidoOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="RaidoHubOptionsSetup{THub}"/> class.
        /// </summary>
        /// <param name="options">The Raido options.</param>
        public RaidoHubOptionsSetup(IOptions<RaidoOptions> options)
        {
            _raidoOptions = options.Value;
        }

        /// <summary>
        /// Configures the options for the hub.
        /// </summary>
        /// <param name="options">The options to configure.</param>
        public void Configure(RaidoHubOptions<THub> options)
        {
            if (_raidoOptions.GlobalHubFilters != null)
            {
                options.HubFilters = new List<IRaidoHubFilter>(_raidoOptions.GlobalHubFilters);
            }
        }
    }
}
