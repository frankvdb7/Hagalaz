using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Raido.Server
{
    /// <summary>
    /// A context for Raido metrics.
    /// </summary>
    public readonly struct MetricsContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsContext"/> struct.
        /// </summary>
        /// <param name="connectionDurationEnabled">A value indicating whether the connection duration metric is enabled.</param>
        /// <param name="currentConnectionsCounterEnabled">A value indicating whether the current connections counter is enabled.</param>
        public MetricsContext(bool connectionDurationEnabled, bool currentConnectionsCounterEnabled)
            : this(
                connectionDurationEnabled,
                currentConnectionsCounterEnabled,
                inboundDispatchCountEnabled: false,
                inboundDispatchDurationEnabled: false,
                outboundWriteCountEnabled: false,
                outboundWriteDurationEnabled: false,
                reconnectEventEnabled: false,
                reconnectDurationEnabled: false,
                clientTimeoutEnabled: false)
        {
        }

        internal MetricsContext(
            bool connectionDurationEnabled,
            bool currentConnectionsCounterEnabled,
            bool inboundDispatchCountEnabled,
            bool inboundDispatchDurationEnabled,
            bool outboundWriteCountEnabled,
            bool outboundWriteDurationEnabled,
            bool reconnectEventEnabled,
            bool reconnectDurationEnabled,
            bool clientTimeoutEnabled)
        {
            ConnectionDurationEnabled = connectionDurationEnabled;
            CurrentConnectionsCounterEnabled = currentConnectionsCounterEnabled;
            InboundDispatchCountEnabled = inboundDispatchCountEnabled;
            InboundDispatchDurationEnabled = inboundDispatchDurationEnabled;
            OutboundWriteCountEnabled = outboundWriteCountEnabled;
            OutboundWriteDurationEnabled = outboundWriteDurationEnabled;
            ReconnectEventEnabled = reconnectEventEnabled;
            ReconnectDurationEnabled = reconnectDurationEnabled;
            ClientTimeoutEnabled = clientTimeoutEnabled;
        }

        /// <summary>
        /// Gets a value indicating whether the connection duration metric is enabled.
        /// </summary>
        public bool ConnectionDurationEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether the current connections counter is enabled.
        /// </summary>
        public bool CurrentConnectionsCounterEnabled { get; }

        internal bool InboundDispatchCountEnabled { get; }
        internal bool InboundDispatchDurationEnabled { get; }
        internal bool InboundDispatchEnabled => InboundDispatchCountEnabled || InboundDispatchDurationEnabled;
        internal bool OutboundWriteCountEnabled { get; }
        internal bool OutboundWriteDurationEnabled { get; }
        internal bool OutboundWriteEnabled => OutboundWriteCountEnabled || OutboundWriteDurationEnabled;
        internal bool ReconnectEventEnabled { get; }
        internal bool ReconnectDurationEnabled { get; }
        internal bool ReconnectEnabled => ReconnectEventEnabled || ReconnectDurationEnabled;
        internal bool ClientTimeoutEnabled { get; }
    }

    /// <summary>
    /// A class for collecting Raido server metrics.
    /// </summary>
    public sealed class RaidoMetrics : IDisposable
    {
        /// <summary>
        /// The name of the meter.
        /// </summary>
        public const string MeterName = "Raido.Server";

        private readonly Meter _meter;
        private readonly Histogram<double> _connectionDuration;
        private readonly UpDownCounter<long> _currentConnectionsCounter;
        private readonly Counter<long> _inboundDispatchCount;
        private readonly Histogram<double> _inboundDispatchDuration;
        private readonly Counter<long> _outboundWriteCount;
        private readonly Histogram<double> _outboundWriteDuration;
        private readonly Counter<long> _reconnectEvents;
        private readonly Histogram<double> _reconnectDuration;
        private readonly Counter<long> _clientTimeouts;
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="RaidoMetrics"/> class.
        /// </summary>
        /// <param name="meterFactory">The meter factory.</param>
        public RaidoMetrics(IMeterFactory meterFactory)
        {
            _meter = meterFactory.Create(MeterName);
            _timeProvider = TimeProvider.System;

            _connectionDuration = _meter.CreateHistogram<double>("raido.server.connection.duration",
                unit: "s",
                description: "The duration of connections on the server.");

            _currentConnectionsCounter = _meter.CreateUpDownCounter<long>("raido.server.active_connections",
                unit: "{connection}",
                description: "The number of active connections on the server.");

            _inboundDispatchCount = _meter.CreateCounter<long>("raido.server.inbound_messages",
                unit: "{message}",
                description: "The number of inbound Raido messages dispatched.");

            _inboundDispatchDuration = _meter.CreateHistogram<double>("raido.server.inbound_message.duration",
                unit: "s",
                description: "The duration of inbound Raido message dispatch.");

            _outboundWriteCount = _meter.CreateCounter<long>("raido.server.outbound_messages",
                unit: "{message}",
                description: "The number of outbound Raido message writes attempted per connection.");

            _outboundWriteDuration = _meter.CreateHistogram<double>("raido.server.outbound_message.duration",
                unit: "s",
                description: "The duration of outbound Raido message writes per connection.");

            _reconnectEvents = _meter.CreateCounter<long>("raido.server.reconnect.events",
                unit: "{event}",
                description: "The number of stateful Raido reconnect lifecycle events.");

            _reconnectDuration = _meter.CreateHistogram<double>("raido.server.reconnect.duration",
                unit: "s",
                description: "The duration of stateful Raido reconnect waits.");

            _clientTimeouts = _meter.CreateCounter<long>("raido.server.client_timeouts",
                unit: "{connection}",
                description: "The number of Raido connections terminated by client timeout.");
        }

        /// <summary>
        /// Records the start of a new connection.
        /// </summary>
        /// <param name="metricsContext">The metrics context.</param>
        public void ConnectionStart(in MetricsContext metricsContext)
        {
            if (metricsContext.CurrentConnectionsCounterEnabled)
            {
                _currentConnectionsCounter.Add(1);
            }
        }

        /// <summary>
        /// Records the stop of a connection.
        /// </summary>
        /// <param name="metricsContext">The metrics context.</param>
        /// <param name="startTimestamp">The start timestamp of the connection.</param>
        /// <param name="currentTimestamp">The current timestamp.</param>
        public void ConnectionStop(in MetricsContext metricsContext, long startTimestamp, long currentTimestamp)
        {
            if (metricsContext.ConnectionDurationEnabled)
            {
                var duration = _timeProvider.GetElapsedTime(startTimestamp, currentTimestamp);
                _connectionDuration.Record(duration.TotalSeconds);
                _currentConnectionsCounter.Add(-1);
            }
        }

        internal void RecordInboundDispatch(
            in MetricsContext metricsContext,
            string messageType,
            string outcome,
            double durationSeconds)
        {
            if (!metricsContext.InboundDispatchEnabled)
            {
                return;
            }

            var tags = new TagList
            {
                { "message.type", messageType },
                { "outcome", outcome }
            };

            if (metricsContext.InboundDispatchCountEnabled)
            {
                _inboundDispatchCount.Add(1, tags);
            }

            if (metricsContext.InboundDispatchDurationEnabled)
            {
                _inboundDispatchDuration.Record(durationSeconds, tags);
            }
        }

        internal void RecordOutboundWrite(
            in MetricsContext metricsContext,
            string messageType,
            string outcome,
            double durationSeconds)
        {
            if (!metricsContext.OutboundWriteEnabled)
            {
                return;
            }

            var tags = new TagList
            {
                { "message.type", messageType },
                { "outcome", outcome }
            };

            if (metricsContext.OutboundWriteCountEnabled)
            {
                _outboundWriteCount.Add(1, tags);
            }

            if (metricsContext.OutboundWriteDurationEnabled)
            {
                _outboundWriteDuration.Record(durationSeconds, tags);
            }
        }

        internal void RecordReconnectEvent(in MetricsContext metricsContext, string outcome)
        {
            if (!metricsContext.ReconnectEventEnabled)
            {
                return;
            }

            var tags = new TagList { { "outcome", outcome } };
            _reconnectEvents.Add(1, tags);
        }

        internal void RecordReconnectCompletion(
            in MetricsContext metricsContext,
            string outcome,
            double durationSeconds)
        {
            if (!metricsContext.ReconnectEnabled)
            {
                return;
            }

            var tags = new TagList { { "outcome", outcome } };
            if (metricsContext.ReconnectEventEnabled)
            {
                _reconnectEvents.Add(1, tags);
            }

            if (metricsContext.ReconnectDurationEnabled)
            {
                _reconnectDuration.Record(durationSeconds, tags);
            }
        }

        internal void RecordClientTimeout(in MetricsContext metricsContext)
        {
            if (metricsContext.ClientTimeoutEnabled)
            {
                _clientTimeouts.Add(1);
            }
        }

        /// <inheritdoc />
        public void Dispose() => _meter.Dispose();

        /// <summary>
        /// Creates a new metrics context.
        /// </summary>
        /// <returns>A new <see cref="MetricsContext"/>.</returns>
        public MetricsContext CreateContext() => new(
            _connectionDuration.Enabled,
            _currentConnectionsCounter.Enabled,
            _inboundDispatchCount.Enabled,
            _inboundDispatchDuration.Enabled,
            _outboundWriteCount.Enabled,
            _outboundWriteDuration.Enabled,
            _reconnectEvents.Enabled,
            _reconnectDuration.Enabled,
            _clientTimeouts.Enabled);
    }
}
