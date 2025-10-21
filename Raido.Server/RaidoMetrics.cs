using System;
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
        {
            ConnectionDurationEnabled = connectionDurationEnabled;
            CurrentConnectionsCounterEnabled = currentConnectionsCounterEnabled;
        }

        /// <summary>
        /// Gets a value indicating whether the connection duration metric is enabled.
        /// </summary>
        public bool ConnectionDurationEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether the current connections counter is enabled.
        /// </summary>
        public bool CurrentConnectionsCounterEnabled { get; }
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

        /// <inheritdoc />
        public void Dispose() => _meter.Dispose();

        /// <summary>
        /// Creates a new metrics context.
        /// </summary>
        /// <returns>A new <see cref="MetricsContext"/>.</returns>
        public MetricsContext CreateContext() => new(_connectionDuration.Enabled, _currentConnectionsCounter.Enabled);
    }
}