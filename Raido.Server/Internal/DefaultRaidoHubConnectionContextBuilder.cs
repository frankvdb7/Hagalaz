using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;

namespace Raido.Server.Internal
{
    internal class DefaultRaidoHubConnectionContextBuilder : IRaidoHubConnectionContextBuilder,
        IRaidoHubConnectionContextBuilderConnection,
        IRaidoHubConnectionContextBuilderProtocol, IRaidoHubConnectionContextBuilderOptional, IRaidoHubConnectionContextBuilderBuild
    {
        private readonly IServiceProvider _serviceProvider;
        private ConnectionContext _connection = null!;
        private IRaidoProtocol _protocol = null!;
        private TimeSpan? _keepAliveInterval;
        private TimeSpan? _clientTimeoutInterval;
        private bool _statefulReconnect;

        public DefaultRaidoHubConnectionContextBuilder(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public IRaidoHubConnectionContextBuilderConnection Create() => new DefaultRaidoHubConnectionContextBuilder(_serviceProvider);

        public IRaidoHubConnectionContextBuilderProtocol WithConnection(ConnectionContext connection)
        {
            _connection = connection;
            return this;
        }

        public IRaidoHubConnectionContextBuilderOptional WithProtocol(IRaidoProtocol protocol)
        {
            _protocol = protocol;
            return this;
        }

        public IRaidoHubConnectionContextBuilderOptional WithProtocol<TProtocol>() where TProtocol : IRaidoProtocol
        {
            _protocol = _serviceProvider.GetRequiredService<TProtocol>();
            return this;
        }

        public IRaidoHubConnectionContextBuilderOptional WithKeepAliveInterval(TimeSpan interval)
        {
            _keepAliveInterval = interval;
            return this;
        }

        public IRaidoHubConnectionContextBuilderOptional WithClientTimeoutInterval(TimeSpan interval)
        {
            _clientTimeoutInterval = interval;
            return this;
        }

        public IRaidoHubConnectionContextBuilderOptional WithStatefulReconnect()
        {
            _statefulReconnect = true;
            return this;
        }

        public RaidoHubConnectionContext Build()
        {
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            var options = _serviceProvider.GetRequiredService<IOptions<RaidoOptions>>();
            var contextOptions = new RaidoHubConnectionContextOptions()
            {
                KeepAliveInterval = _keepAliveInterval ?? options.Value.KeepAliveInterval.GetValueOrDefault(),
                ClientTimeoutInterval = _clientTimeoutInterval ?? options.Value.ClientTimeoutInterval.GetValueOrDefault(),
                StatefulReconnectEnabled = _statefulReconnect,
                StatefulReconnectTimeout = options.Value.StatefulReconnectTimeout.GetValueOrDefault(RaidoOptionsSetup.DefaultStatefulReconnectTimeout)
            };

            return new RaidoHubConnectionContext(_connection, contextOptions, loggerFactory)
            {
                Protocol = _protocol,
                OriginalActivity = Activity.Current
            };
        }
    }
}