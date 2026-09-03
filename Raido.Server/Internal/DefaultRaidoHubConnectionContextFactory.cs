using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;

namespace Raido.Server.Internal
{
    internal sealed class DefaultRaidoHubConnectionContextFactory : IRaidoHubConnectionContextFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IOptions<RaidoOptions> _options;

        public DefaultRaidoHubConnectionContextFactory(
            ILoggerFactory loggerFactory,
            IOptions<RaidoOptions> options)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public RaidoHubConnectionContext Create(
            ConnectionContext connection,
            IRaidoProtocol protocol,
            TimeSpan? keepAliveInterval = null,
            TimeSpan? clientTimeoutInterval = null,
            bool statefulReconnect = false)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(protocol);

            var options = _options.Value;
            var contextOptions = new RaidoConnectionContextOptions
            {
                KeepAliveInterval = keepAliveInterval ?? options.KeepAliveInterval.GetValueOrDefault(),
                ClientTimeoutInterval = clientTimeoutInterval ?? options.ClientTimeoutInterval.GetValueOrDefault(),
                StatefulReconnectEnabled = statefulReconnect,
                StatefulReconnectTimeout = options.StatefulReconnectTimeout.GetValueOrDefault(RaidoOptionsSetup.DefaultStatefulReconnectTimeout)
            };

            var tcpConnection = new RaidoTcpConnectionContext(contextOptions, _loggerFactory);
            if (!tcpConnection.TryAttachPhysicalConnection(connection))
            {
                throw new InvalidOperationException("The initial physical connection could not be activated.");
            }

            return new RaidoHubConnectionContext(tcpConnection, contextOptions, _loggerFactory, TimeProvider.System)
            {
                Protocol = protocol,
                OriginalActivity = Activity.Current
            };
        }
    }
}
