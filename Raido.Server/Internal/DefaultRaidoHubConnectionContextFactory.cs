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
            bool statefulReconnect)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(protocol);

            var options = _options.Value;
            return Create(connection, protocol, new RaidoConnectionContextOptions
            {
                KeepAliveInterval = options.KeepAliveInterval.GetValueOrDefault(),
                ClientTimeoutInterval = options.ClientTimeoutInterval.GetValueOrDefault(),
                StatefulReconnectEnabled = statefulReconnect,
                StatefulReconnectTimeout = options.StatefulReconnectTimeout.GetValueOrDefault(RaidoOptionsSetup.DefaultStatefulReconnectTimeout)
            });
        }

        internal RaidoHubConnectionContext Create(
            ConnectionContext connection,
            IRaidoProtocol protocol,
            RaidoConnectionContextOptions contextOptions)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(protocol);
            ArgumentNullException.ThrowIfNull(contextOptions);

            var tcpConnection = new RaidoTcpConnectionContext(contextOptions, _loggerFactory);
            if (!tcpConnection.TryAttachPhysicalConnection(connection))
            {
                throw new InvalidOperationException("The initial physical connection could not be activated.");
            }

            var hubConnection = new RaidoHubConnectionContext(
                tcpConnection,
                contextOptions,
                protocol,
                _loggerFactory,
                TimeProvider.System)
            {
                OriginalActivity = Activity.Current
            };

            return hubConnection;
        }
    }
}
