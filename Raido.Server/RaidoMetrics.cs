using System;
using System.Diagnostics.Metrics;

namespace Raido.Server
{
    public readonly struct MetricsContext
    {
        public MetricsContext(bool connectionDurationEnabled, bool currentConnectionsCounterEnabled)
        {
            ConnectionDurationEnabled = connectionDurationEnabled;
            CurrentConnectionsCounterEnabled = currentConnectionsCounterEnabled;
        }

        public bool ConnectionDurationEnabled { get; }
        public bool CurrentConnectionsCounterEnabled { get; }
    }

    public sealed class RaidoMetrics : IDisposable
    {
        public const string MeterName = "Raido.Server";

        private readonly Meter _meter;
        private readonly Histogram<double> _connectionDuration;
        private readonly UpDownCounter<long> _currentConnectionsCounter;
        private readonly TimeProvider _timeProvider;

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
        }

        public void ConnectionStart(in MetricsContext metricsContext)
        {
            if (metricsContext.CurrentConnectionsCounterEnabled)
            {
                _currentConnectionsCounter.Add(1);
            }
        }

        public void ConnectionStop(in MetricsContext metricsContext, long startTimestamp, long currentTimestamp)
        {
            if (metricsContext.ConnectionDurationEnabled)
            {
                var duration = _timeProvider.GetElapsedTime(startTimestamp, currentTimestamp);
                _connectionDuration.Record(duration.TotalSeconds);
                _currentConnectionsCounter.Add(-1);
            }
        }


        public void Dispose() => _meter.Dispose();

        public MetricsContext CreateContext() => new(_connectionDuration.Enabled, _currentConnectionsCounter.Enabled);
    }
}