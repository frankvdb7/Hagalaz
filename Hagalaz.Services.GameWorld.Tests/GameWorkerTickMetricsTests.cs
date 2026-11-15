using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
[DoNotParallelize] // The tick instruments are process-wide, so concurrent workers would contaminate the listener.
public sealed class GameWorkerTickMetricsTests
{
    private const string MeterName = "Hagalaz.Services.GameWorld";
    private static readonly TimeSpan Budget = TimeSpan.FromMilliseconds(600);
    private static readonly string[] AllPhases =
        ["scheduler", "major_update", "client_prepare", "client_update", "client_reset", "housekeeping"];

    [TestMethod]
    public async Task ExecuteTickAsync_WithinBudget_RecordsOneSuccessfulTickAndEveryPhaseInOrder()
    {
        var time = new ManualTimeProvider();
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ => time.Advance(TimeSpan.FromMilliseconds(100)));
        using var spy = new MeterListenerSpy(MeterName);
        var worker = CreateWorker(region, time);

        await worker.ExecuteTickAsync(CancellationToken.None);

        var tick = Single(spy, "hagalaz.gameworld.tick");
        Assert.AreEqual("success", tick.Tags["outcome"]);
        Assert.AreEqual(1, tick.Value);
        Assert.AreEqual(0.1, Single(spy, "hagalaz.gameworld.tick.duration").Value, 1e-9);
        Assert.IsEmpty(For(spy, "hagalaz.gameworld.tick.overrun"));
        Assert.IsEmpty(For(spy, "hagalaz.gameworld.tick.interval"));
        var phases = For(spy, "hagalaz.gameworld.tick.phase.duration");
        CollectionAssert.AreEqual(AllPhases, phases.Select(item => (string)item.Tags["phase"]!).ToArray());
        Assert.AreEqual(0.1, phases[1].Value, 1e-9);
    }

    [TestMethod]
    public async Task ExecuteTickAsync_AtBudget_DoesNotRecordOverrun()
    {
        var time = new ManualTimeProvider();
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ => time.Advance(Budget));
        using var spy = new MeterListenerSpy(MeterName);

        await CreateWorker(region, time).ExecuteTickAsync(CancellationToken.None);

        Assert.IsEmpty(For(spy, "hagalaz.gameworld.tick.overrun"));
    }

    [TestMethod]
    public async Task ExecuteTickAsync_BeyondBudget_RecordsOverrun()
    {
        var time = new ManualTimeProvider();
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ => time.Advance(Budget + TimeSpan.FromMilliseconds(1)));
        using var spy = new MeterListenerSpy(MeterName);

        await CreateWorker(region, time).ExecuteTickAsync(CancellationToken.None);

        Assert.AreEqual(1, Single(spy, "hagalaz.gameworld.tick.overrun").Value);
        Assert.AreEqual("success", Single(spy, "hagalaz.gameworld.tick").Tags["outcome"]);
    }

    [TestMethod]
    public async Task ExecuteTickAsync_WhenPhaseThrows_RecordsFailureOverrunAndOnlyEnteredPhases()
    {
        var time = new ManualTimeProvider();
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorClientPrepareUpdateTick()).Do(_ =>
        {
            time.Advance(TimeSpan.FromSeconds(1));
            throw new InvalidOperationException("prepare failed");
        });
        using var spy = new MeterListenerSpy(MeterName);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => CreateWorker(region, time).ExecuteTickAsync(CancellationToken.None));

        Assert.AreEqual("failure", Single(spy, "hagalaz.gameworld.tick").Tags["outcome"]);
        Assert.AreEqual(1.0, Single(spy, "hagalaz.gameworld.tick.duration").Value, 1e-9);
        Assert.AreEqual(1, Single(spy, "hagalaz.gameworld.tick.overrun").Value);
        var phases = For(spy, "hagalaz.gameworld.tick.phase.duration");
        CollectionAssert.AreEqual(
            new[] { "scheduler", "major_update", "client_prepare", "client_reset" },
            phases.Select(item => (string)item.Tags["phase"]!).ToArray());
        Assert.AreEqual(1.0, phases[2].Value, 1e-9);
    }

    [TestMethod]
    public async Task ExecuteTickAsync_WhenWorkerTokenIsCancelled_RecordsCancelledAndOnlyEnteredPhases()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var spy = new MeterListenerSpy(MeterName);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => CreateWorker(Substitute.For<IMapRegion>(), new ManualTimeProvider()).ExecuteTickAsync(cancellation.Token));

        Assert.AreEqual("cancelled", Single(spy, "hagalaz.gameworld.tick").Tags["outcome"]);
        Assert.HasCount(1, For(spy, "hagalaz.gameworld.tick.duration"));
        Assert.AreEqual("scheduler", Single(spy, "hagalaz.gameworld.tick.phase.duration").Tags["phase"]);
    }

    [TestMethod]
    public async Task ExecuteTickAsync_WhenForeignCancellationEscapes_RecordsFailure()
    {
        using var foreign = new CancellationTokenSource();
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick())
            .Do(_ => throw new OperationCanceledException("foreign", foreign.Token));
        using var spy = new MeterListenerSpy(MeterName);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => CreateWorker(region, new ManualTimeProvider()).ExecuteTickAsync(CancellationToken.None));

        Assert.AreEqual("failure", Single(spy, "hagalaz.gameworld.tick").Tags["outcome"]);
    }

    [TestMethod]
    public async Task ExecuteTickAsync_RecordsIntervalBetweenActualTickStarts()
    {
        var time = new ManualTimeProvider();
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ => time.Advance(TimeSpan.FromMilliseconds(50)));
        using var spy = new MeterListenerSpy(MeterName);
        var worker = CreateWorker(region, time);

        await worker.ExecuteTickAsync(CancellationToken.None);
        time.Advance(TimeSpan.FromMilliseconds(500));
        await worker.ExecuteTickAsync(CancellationToken.None);

        // Start-to-start: tick work (50 ms) plus the gap (500 ms); neither the delay nor the work alone.
        Assert.AreEqual(0.55, Single(spy, "hagalaz.gameworld.tick.interval").Value, 1e-9);
        Assert.HasCount(2, For(spy, "hagalaz.gameworld.tick"));
    }

    [TestMethod]
    public async Task ExecuteTickAsync_UsesDocumentedInstrumentUnitsAndBoundedTags()
    {
        using var spy = new MeterListenerSpy(MeterName);
        var worker = CreateWorker(Substitute.For<IMapRegion>(), new ManualTimeProvider());

        await worker.ExecuteTickAsync(CancellationToken.None);
        await worker.ExecuteTickAsync(CancellationToken.None);

        var units = spy.Measurements.GroupBy(item => item.InstrumentName)
            .ToDictionary(group => group.Key, group => group.First().Unit);
        Assert.AreEqual("s", units["hagalaz.gameworld.tick.duration"]);
        Assert.AreEqual("s", units["hagalaz.gameworld.tick.phase.duration"]);
        Assert.AreEqual("s", units["hagalaz.gameworld.tick.interval"]);
        Assert.AreEqual("{tick}", units["hagalaz.gameworld.tick"]);
        var tagKeys = spy.Measurements.SelectMany(item => item.Tags.Keys).Distinct().Order().ToArray();
        CollectionAssert.AreEqual(new[] { "outcome", "phase" }, tagKeys);
    }

    [TestMethod]
    public async Task ExecuteTickAsync_WithoutListeners_ReadsNoTimestamps()
    {
        var time = new ManualTimeProvider();
        var worker = CreateWorker(Substitute.For<IMapRegion>(), time);

        await worker.ExecuteTickAsync(CancellationToken.None);
        await worker.ExecuteTickAsync(CancellationToken.None);

        Assert.AreEqual(0, time.TimestampReads);
    }

    private static List<MetricMeasurement> For(MeterListenerSpy spy, string name) =>
        spy.Measurements.Where(item => item.InstrumentName == name).ToList();

    private static MetricMeasurement Single(MeterListenerSpy spy, string name) =>
        For(spy, name).Single();

    private static GameWorkerService CreateWorker(IMapRegion region, TimeProvider time)
    {
        region.State.Returns(MapRegionState.Ready);
        var regionService = Substitute.For<IMapRegionService>();
        regionService.FindReadyRegions().Returns([region]);
        regionService.FindAllDimensions().Returns([]);
        var store = Substitute.For<ICharacterStore>();
#pragma warning disable CA2012 // NSubstitute consumes the configured ValueTask exactly once.
        store.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(_ =>
            new ValueTask<IReadOnlyDictionary<int, ICharacter>>(new Dictionary<int, ICharacter>()));
#pragma warning restore CA2012
        return new GameWorkerService(
            Substitute.For<IRsTaskService>(),
            regionService,
            new MapRegionBackgroundService(regionService, Substitute.For<ILogger<MapRegionBackgroundService>>()),
            store,
            Options.Create(new GameServerOptions
            {
                AuthenticationToken = string.Empty,
                ClientRevision = 0,
                ClientRevisionPatch = 0,
                TickTimeSpan = Budget
            }),
            Substitute.For<ILogger<GameWorkerService>>(),
            time);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;
        private int _reads;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public int TimestampReads => Volatile.Read(ref _reads);

        public override long GetTimestamp()
        {
            Interlocked.Increment(ref _reads);
            return Interlocked.Read(ref _timestamp);
        }

        public void Advance(TimeSpan amount) => Interlocked.Add(ref _timestamp, amount.Ticks);
    }
}
