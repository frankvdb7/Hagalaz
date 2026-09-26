using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace Hagalaz.Services.GameWorld.Tests;

internal sealed class MeterListenerSpy : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly object _sync = new();

    public MeterListenerSpy(string meterName)
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
        _listener.Start();
    }

    public List<MetricMeasurement> Measurements { get; } = [];

    public void Dispose() => _listener.Dispose();

    private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var copiedTags = new Dictionary<string, object?>(tags.Length);
        foreach (var tag in tags)
        {
            copiedTags[tag.Key] = tag.Value;
        }

        lock (_sync)
        {
            Measurements.Add(new MetricMeasurement(instrument.Name, value, copiedTags));
        }
    }
}

internal sealed record MetricMeasurement(string InstrumentName, double Value, IReadOnlyDictionary<string, object?> Tags);
