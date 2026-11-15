using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Hagalaz.Services.GameWorld.Metrics;

internal static class GameWorldMetrics
{
    public const string MeterName = "Hagalaz.Services.GameWorld";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> RegionLoads = Meter.CreateCounter<long>(
        "hagalaz.gameworld.map.region.load",
        "{load}");
    private static readonly Histogram<double> RegionLoadDuration = Meter.CreateHistogram<double>(
        "hagalaz.gameworld.map.region.load.duration",
        "s");
    private static readonly Counter<long> DefinitionResolutions = Meter.CreateCounter<long>(
        "hagalaz.gameworld.gameobject.definition.resolve",
        "{batch}");
    private static readonly Histogram<double> DefinitionResolutionDuration = Meter.CreateHistogram<double>(
        "hagalaz.gameworld.gameobject.definition.resolve.duration",
        "s");
    private static readonly Histogram<long> DefinitionResolutionBatchSize = Meter.CreateHistogram<long>(
        "hagalaz.gameworld.gameobject.definition.resolve.batch_size",
        "{definition}");
    private static readonly Counter<long> DefinitionCacheLookups = Meter.CreateCounter<long>(
        "hagalaz.gameworld.gameobject.definition.cache.lookup",
        "{lookup}");
    private static readonly Histogram<double> TickDuration = Meter.CreateHistogram<double>(
        "hagalaz.gameworld.tick.duration",
        "s");
    private static readonly Counter<long> Ticks = Meter.CreateCounter<long>(
        "hagalaz.gameworld.tick",
        "{tick}");
    private static readonly Counter<long> TickOverruns = Meter.CreateCounter<long>(
        "hagalaz.gameworld.tick.overrun",
        "{tick}");
    private static readonly Histogram<double> TickPhaseDuration = Meter.CreateHistogram<double>(
        "hagalaz.gameworld.tick.phase.duration",
        "s");
    private static readonly Histogram<double> TickInterval = Meter.CreateHistogram<double>(
        "hagalaz.gameworld.tick.interval",
        "s");

    public static bool TickMetricsEnabled =>
        TickDuration.Enabled || Ticks.Enabled || TickOverruns.Enabled || TickInterval.Enabled;

    public static void RecordTickInterval(TimeSpan interval) => TickInterval.Record(interval.TotalSeconds);

    public static void RecordTick(string outcome, TimeSpan duration, bool overrun)
    {
        var tags = new TagList { { "outcome", outcome } };
        Ticks.Add(1, tags);
        TickDuration.Record(duration.TotalSeconds);
        if (overrun)
        {
            TickOverruns.Add(1);
        }
    }

    public static TickPhaseScope BeginTickPhase(string phase, TimeProvider timeProvider) =>
        new(phase, timeProvider, TickPhaseDuration.Enabled);

    internal static void RecordTickPhase(string phase, TimeSpan duration) =>
        TickPhaseDuration.Record(duration.TotalSeconds, new TagList { { "phase", phase } });

    public static void RecordRegionLoad(string outcome, double durationSeconds)
    {
        var tags = new TagList { { "outcome", outcome } };
        RegionLoads.Add(1, tags);
        RegionLoadDuration.Record(durationSeconds, tags);
    }

    public static void RecordDefinitionResolution(string outcome, int? batchSize, double durationSeconds)
    {
        var tags = new TagList { { "outcome", outcome } };
        DefinitionResolutions.Add(1, tags);
        DefinitionResolutionDuration.Record(durationSeconds, tags);
        if (batchSize is int size)
        {
            DefinitionResolutionBatchSize.Record(size);
        }
    }

    public static void RecordDefinitionCacheLookup(string outcome)
    {
        var tags = new TagList { { "outcome", outcome } };
        DefinitionCacheLookups.Add(1, tags);
    }

}

internal static class TickOutcome
{
    public const string Success = "success";
    public const string Failure = "failure";
    public const string Cancelled = "cancelled";
}

internal static class TickPhase
{
    public const string Scheduler = "scheduler";
    public const string MajorUpdate = "major_update";
    public const string ClientPrepare = "client_prepare";
    public const string ClientUpdate = "client_update";
    public const string ClientReset = "client_reset";
    public const string Housekeeping = "housekeeping";
}

internal readonly struct TickPhaseScope : IDisposable
{
    private readonly string _phase;
    private readonly TimeProvider _timeProvider;
    private readonly long _start;
    private readonly bool _active;

    public TickPhaseScope(string phase, TimeProvider timeProvider, bool active)
    {
        _phase = phase;
        _timeProvider = timeProvider;
        _active = active;
        _start = active ? timeProvider.GetTimestamp() : 0;
    }

    public void Dispose()
    {
        if (_active)
        {
            GameWorldMetrics.RecordTickPhase(_phase, _timeProvider.GetElapsedTime(_start));
        }
    }
}
