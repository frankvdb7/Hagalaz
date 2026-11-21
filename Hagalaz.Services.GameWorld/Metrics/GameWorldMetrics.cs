using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;

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

    private static readonly Counter<long> TaskTicks = Meter.CreateCounter<long>(
        "hagalaz.gameworld.tasks.processed",
        "{tick}");
    private static readonly Histogram<double> TaskTickDuration = Meter.CreateHistogram<double>(
        "hagalaz.gameworld.tasks.tick.duration",
        "s");
    private static readonly Histogram<double> RegionLoadQueueDuration = Meter.CreateHistogram<double>(
        "hagalaz.gameworld.region.load.queue.duration",
        "s");

    // Snapshot capture and flush hand-off only. Durable commits are measured by the Characters service.
    private static readonly Histogram<double> CharacterSnapshotDuration = Meter.CreateHistogram<double>(
        "hagalaz.gameworld.character.snapshot.duration",
        "s");
    private static readonly Histogram<double> CharacterFlushDuration = Meter.CreateHistogram<double>(
        "hagalaz.gameworld.character.flush.duration",
        "s");
    private static readonly Counter<long> CharacterFlushes = Meter.CreateCounter<long>(
        "hagalaz.gameworld.character.flush",
        "{flush}");
    private static readonly Counter<long> CharacterLogoutRecoveries = Meter.CreateCounter<long>(
        "hagalaz.gameworld.character.logout.recovery",
        "{recovery}");

    private static readonly Histogram<int> TickRegions = Meter.CreateHistogram<int>(
        "hagalaz.gameworld.tick.regions",
        "{region}");

    // Published by the owners of the authoritative state after a successful transition.
    // The observable callbacks only read these fields, never a store or region collection.
    private static int _registeredCharacters;
    private static int _registeredNpcs;
    private static int _readyRegions;
    private static int _idleRegions;
    private static int _activeTasks;
    private static int _inFlightRegionLoads;

    // Registered characters are those held by the character store. World admission registers a
    // character before its session is committed, so this is not the authenticated player population.
    private static readonly ObservableGauge<int> RegisteredCharacters = Meter.CreateObservableGauge(
        "hagalaz.gameworld.characters.registered",
        () => Volatile.Read(ref _registeredCharacters),
        "{character}");
    private static readonly ObservableGauge<int> RegisteredNpcs = Meter.CreateObservableGauge(
        "hagalaz.gameworld.npcs.registered",
        () => Volatile.Read(ref _registeredNpcs),
        "{npc}");
    // Ready regions are the ready active regions found by the most recent game tick.
    private static readonly ObservableGauge<int> ReadyRegions = Meter.CreateObservableGauge(
        "hagalaz.gameworld.regions.ready",
        () => Volatile.Read(ref _readyRegions),
        "{region}");
    private static readonly ObservableGauge<int> IdleRegions = Meter.CreateObservableGauge(
        "hagalaz.gameworld.regions.idle",
        () => Volatile.Read(ref _idleRegions),
        "{region}");

    // Active tasks are the tasks the task service retains after its latest tick, pending tasks excluded.
    private static readonly ObservableGauge<int> ActiveTasks = Meter.CreateObservableGauge(
        "hagalaz.gameworld.tasks.active",
        () => Volatile.Read(ref _activeTasks),
        "{task}");
    // In-flight region loads are unique deduplicated requests, queued or executing.
    private static readonly ObservableGauge<int> InFlightRegionLoads = Meter.CreateObservableGauge(
        "hagalaz.gameworld.region.load.inflight",
        () => Volatile.Read(ref _inFlightRegionLoads),
        "{load}");

    public static void CharacterRegistered() => Interlocked.Increment(ref _registeredCharacters);

    public static void CharacterUnregistered() => Interlocked.Decrement(ref _registeredCharacters);

    public static void NpcRegistered() => Interlocked.Increment(ref _registeredNpcs);

    public static void NpcUnregistered() => Interlocked.Decrement(ref _registeredNpcs);

    public static void RegionSuspended() => Interlocked.Increment(ref _idleRegions);

    public static void IdleRegionLeft() => Interlocked.Decrement(ref _idleRegions);

    public static void RecordTickRegions(int readyRegions)
    {
        Volatile.Write(ref _readyRegions, readyRegions);
        TickRegions.Record(readyRegions);
    }

    public static bool TaskTickDurationEnabled => TaskTickDuration.Enabled;

    public static void RecordTaskTick(int processedTasks, int activeTasks, TimeSpan? duration)
    {
        Volatile.Write(ref _activeTasks, activeTasks);
        if (processedTasks > 0)
        {
            TaskTicks.Add(processedTasks);
        }

        if (duration is { } elapsed)
        {
            TaskTickDuration.Record(elapsed.TotalSeconds);
        }
    }

    public static bool RegionLoadQueueDurationEnabled => RegionLoadQueueDuration.Enabled;

    public static void RegionLoadQueued() => Interlocked.Increment(ref _inFlightRegionLoads);

    public static void RegionLoadsFinished(int count) => Interlocked.Add(ref _inFlightRegionLoads, -count);

    public static void RecordRegionLoadQueueDuration(TimeSpan duration) =>
        RegionLoadQueueDuration.Record(duration.TotalSeconds);

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

    public static void RecordCharacterSnapshot(string outcome, TimeSpan duration) =>
        CharacterSnapshotDuration.Record(duration.TotalSeconds, new TagList { { "outcome", outcome } });

    public static void RecordCharacterFlush(string mode, string outcome, TimeSpan duration)
    {
        var tags = new TagList { { "mode", mode }, { "outcome", outcome } };
        CharacterFlushes.Add(1, tags);
        CharacterFlushDuration.Record(duration.TotalSeconds, tags);
    }

    public static void RecordCharacterLogoutRecovery(string outcome) =>
        CharacterLogoutRecoveries.Add(1, new TagList { { "outcome", outcome } });

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

internal static class CharacterFlushMode
{
    public const string Periodic = "periodic";
    public const string Shutdown = "shutdown";
}

internal static class LogoutRecoveryOutcome
{
    public const string Completed = "completed";
    public const string Incomplete = "incomplete";
    public const string Failed = "failed";
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
