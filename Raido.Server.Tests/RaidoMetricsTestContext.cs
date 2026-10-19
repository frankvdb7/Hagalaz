using System.Collections.Generic;
using System.Diagnostics.Metrics;
using NSubstitute;

namespace Raido.Server.Tests;

internal sealed class RaidoMetricsTestContext : IDisposable
{
    private readonly Meter _meter;

    public RaidoMetricsTestContext()
    {
        _meter = new Meter($"Raido.Server.Tests.{Guid.NewGuid():N}");
        Listener = new RaidoMetricsTestListener(_meter.Name);

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(_meter);
        Metrics = new RaidoMetrics(meterFactory);
    }

    public RaidoMetrics Metrics { get; }

    public RaidoMetricsTestListener Listener { get; }

    public void Dispose()
    {
        Listener.Dispose();
        Metrics.Dispose();
    }
}

internal sealed class RaidoMetricsTestListener : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly string _meterName;
    private readonly object _sync = new();
    private readonly List<MetricMeasurement> _measurements = [];
    private readonly List<Instrument> _instruments = [];

    public RaidoMetricsTestListener(string meterName)
    {
        _meterName = meterName;
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == _meterName)
            {
                lock (_sync)
                {
                    _instruments.Add(instrument);
                }

                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>(Record);
        _listener.SetMeasurementEventCallback<double>(Record);
        _listener.Start();
    }

    public IReadOnlyList<Instrument> Instruments
    {
        get
        {
            lock (_sync)
            {
                return [.. _instruments];
            }
        }
    }

    public IReadOnlyList<MetricMeasurement> Measurements
    {
        get
        {
            lock (_sync)
            {
                return [.. _measurements];
            }
        }
    }

    public void Dispose() => _listener.Dispose();

    private void Record(Instrument instrument, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state) =>
        Record(instrument, (double)value, tags, state);

    private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
    {
        var copiedTags = new Dictionary<string, object?>(tags.Length);
        foreach (var tag in tags)
        {
            copiedTags[tag.Key] = tag.Value;
        }

        lock (_sync)
        {
            _measurements.Add(new MetricMeasurement(instrument.Name, value, copiedTags));
        }
    }
}

internal sealed record MetricMeasurement(
    string InstrumentName,
    double Value,
    IReadOnlyDictionary<string, object?> Tags);
