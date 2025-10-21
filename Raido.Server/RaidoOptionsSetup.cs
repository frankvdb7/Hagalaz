using System;
using Microsoft.Extensions.Options;

namespace Raido.Server
{
    /// <summary>
    /// Sets up the default options for Raido.
    /// </summary>
    public class RaidoOptionsSetup : IConfigureOptions<RaidoOptions>
    {
        internal static TimeSpan DefaultKeepAliveInterval => TimeSpan.FromSeconds(15);

        internal static TimeSpan DefaultClientTimeoutInterval => TimeSpan.FromSeconds(30);

        internal const int DefaultMaximumMessageSize = 32 * 1024;

        /// <summary>
        /// Configures the Raido options.
        /// </summary>
        /// <param name="options">The options to configure.</param>
        public void Configure(RaidoOptions options)
        {
            if (options.KeepAliveInterval == null)
            {
                // The default keep - alive interval. This is set to exactly half of the default client timeout window,
                // to ensure a ping can arrive in time to satisfy the client timeout.
                options.KeepAliveInterval = DefaultKeepAliveInterval;
            }

            if (options.ClientTimeoutInterval == null)
            {
                options.ClientTimeoutInterval = DefaultClientTimeoutInterval;
            }

            if (options.MaximumReceiveMessageSize == null)
            {
                options.MaximumReceiveMessageSize = DefaultMaximumMessageSize;
            }
        }
    }
}