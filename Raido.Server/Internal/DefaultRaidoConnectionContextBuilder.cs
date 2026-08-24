using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raido.Common.Protocol;

namespace Raido.Server.Internal
{
    internal class DefaultRaidoConnectionContextBuilder : IRaidoConnectionContextBuilder,
        IRaidoConnectionContextBuilderConnection,
        IRaidoConnectionContextBuilderProtocol, IRaidoConnectionContextBuilderOptional, IRaidoConnectionContextBuilderBuild
    {
        private readonly IServiceProvider _serviceProvider;
        private ConnectionContext _connection = null!;
        private IRaidoProtocol _protocol = null!;
        private TimeSpan? _keepAliveInterval;
        private TimeSpan? _clientTimeoutInterval;
        private bool? _statefulReconnectEnabled;
        private TimeSpan? _statefulReconnectGracePeriod;

        public DefaultRaidoConnectionContextBuilder(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public IRaidoConnectionContextBuilderConnection Create() => new DefaultRaidoConnectionContextBuilder(_serviceProvider);

        public IRaidoConnectionContextBuilderProtocol WithConnection(ConnectionContext connection)
        {
            _connection = connection;
            return this;
        }

        public IRaidoConnectionContextBuilderOptional WithProtocol(IRaidoProtocol protocol)
        {
            _protocol = protocol;
            return this;
        }

        public IRaidoConnectionContextBuilderOptional WithProtocol<TProtocol>() where TProtocol : IRaidoProtocol
        {
            _protocol = _serviceProvider.GetRequiredService<TProtocol>();
            return this;
        }

        public IRaidoConnectionContextBuilderOptional WithKeepAliveInterval(TimeSpan interval)
        {
            _keepAliveInterval = interval;
            return this;
        }

        public IRaidoConnectionContextBuilderOptional WithClientTimeoutInterval(TimeSpan interval)
        {
            _clientTimeoutInterval = interval;
            return this;
        }

        public IRaidoConnectionContextBuilderOptional WithStatefulReconnect(TimeSpan? gracePeriod = null)
        {
            _statefulReconnectEnabled = true;
            _statefulReconnectGracePeriod = gracePeriod;
            return this;
        }

        public RaidoConnectionContext Build()
        {
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            var options = _serviceProvider.GetRequiredService<IOptions<RaidoOptions>>();
            var contextOptions = new RaidoConnectionContextOptions()
            {
                KeepAliveInterval = _keepAliveInterval ?? options.Value.KeepAliveInterval.GetValueOrDefault(),
                ClientTimeoutInterval = _clientTimeoutInterval ?? options.Value.ClientTimeoutInterval.GetValueOrDefault(),
                StatefulReconnectEnabled = _statefulReconnectEnabled ?? options.Value.StatefulReconnectEnabled,
                StatefulReconnectGracePeriod = _statefulReconnectGracePeriod ?? options.Value.StatefulReconnectGracePeriod
            };

            var context = new RaidoConnectionContext(_connection, contextOptions, loggerFactory)
            {
                Protocol = _protocol,
                OriginalActivity = Activity.Current
            };

            if (_statefulReconnectEnabled == true)
            {
                context.Features.Get<IRaidoStatefulReconnectFeature>()?.EnableReconnect();
            }

            return context;
        }
    }
}
