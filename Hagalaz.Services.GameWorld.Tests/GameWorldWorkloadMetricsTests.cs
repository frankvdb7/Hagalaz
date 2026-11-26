using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
[DoNotParallelize] // The instruments are process-wide, so concurrent services would contaminate the listener.
public sealed class GameWorldWorkloadMetricsTests
{
    private const string MeterName = "Hagalaz.Services.GameWorld";
    private const string Active = "hagalaz.gameworld.tasks.active";
    private const string Processed = "hagalaz.gameworld.tasks.processed";
    private const string TickDuration = "hagalaz.gameworld.tasks.tick.duration";
    private const string InFlight = "hagalaz.gameworld.region.load.inflight";
    private const string QueueDuration = "hagalaz.gameworld.region.load.queue.duration";

    [TestMethod]
    public void Tick_RecurringTask_CountsEveryInvocationAndDropsTheTaskOnceRemoved()
    {
        var time = new ManualTimeProvider();
        var service = new RsTaskService(NullLogger<RsTaskService>.Instance, time);
        var task = new StubTask { TicksToComplete = 3, OnTick = () => time.Advance(TimeSpan.FromMilliseconds(5)) };
        using var spy = new MeterListenerSpy(MeterName);

        service.Schedule(task);
        service.Tick();
        Assert.AreEqual(1, spy.ObserveGauges()[Active]);
        service.Tick();
        service.Tick();
        Assert.AreEqual(1, spy.ObserveGauges()[Active]);
        service.Tick();

        Assert.AreEqual(0, spy.ObserveGauges()[Active]);
        Assert.AreEqual(3, spy.Measurements.Where(item => item.InstrumentName == Processed).Sum(item => item.Value));
        var durations = spy.Measurements.Where(item => item.InstrumentName == TickDuration).ToList();
        Assert.HasCount(4, durations);
        Assert.AreEqual(0.005, durations[0].Value, 1e-9);
        Assert.AreEqual("s", durations[0].Unit);
        Assert.IsTrue(spy.Measurements.Where(item => item.InstrumentName is Processed or TickDuration).All(item => item.Tags.Count == 0));
    }

    [TestMethod]
    public void Tick_FaultingTask_StillCountsTheInvocationAndRecordsDuration()
    {
        var service = new RsTaskService(NullLogger<RsTaskService>.Instance, new ManualTimeProvider());
        using var spy = new MeterListenerSpy(MeterName);

        service.Schedule(new StubTask { OnTick = () => throw new InvalidOperationException("boom") });
        service.Tick();

        Assert.AreEqual(1, spy.Measurements.Single(item => item.InstrumentName == Processed).Value);
        Assert.HasCount(1, spy.Measurements.Where(item => item.InstrumentName == TickDuration).ToList());
        Assert.AreEqual(1, spy.ObserveGauges()[Active]);
    }

    [TestMethod]
    public void Tick_NoTasks_ExportsZeroActive()
    {
        var service = new RsTaskService(NullLogger<RsTaskService>.Instance, new ManualTimeProvider());
        using var spy = new MeterListenerSpy(MeterName);

        service.Tick();

        Assert.AreEqual(0, spy.ObserveGauges()[Active]);
        Assert.IsEmpty(spy.Measurements.Where(item => item.InstrumentName == Processed).ToList());
    }

    [TestMethod]
    public async Task EnsureLoadedAsync_DeduplicatedWaiters_ShareOneInFlightLoadAndOneQueueSample()
    {
        var time = new ManualTimeProvider();
        var loadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loader = Substitute.For<IMapRegionLoader>();
        loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                loadStarted.TrySetResult();
                await release.Task;
            });
        var region = CreateRegion(1, () => release.Task.IsCompleted);
        using var provider = new ServiceCollection().AddScoped(_ => loader).BuildServiceProvider();
        using var scheduler = CreateScheduler(provider, time);
        using var spy = new MeterListenerSpy(MeterName);
        var baseline = spy.ObserveGauges()[InFlight];

        var first = scheduler.EnsureLoadedAsync([region]);
        time.Advance(TimeSpan.FromMilliseconds(250));
        var second = scheduler.EnsureLoadedAsync([region]);
        Assert.AreEqual(baseline + 1, spy.ObserveGauges()[InFlight]);

        await scheduler.StartAsync(CancellationToken.None);
        await loadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.AreEqual(baseline + 1, spy.ObserveGauges()[InFlight]);

        release.TrySetResult();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
        await scheduler.StopAsync(CancellationToken.None);

        Assert.AreEqual(baseline, spy.ObserveGauges()[InFlight]);
        var sample = spy.Measurements.Single(item => item.InstrumentName == QueueDuration);
        Assert.AreEqual(0.25, sample.Value, 1e-9);
        Assert.AreEqual("s", sample.Unit);
        Assert.IsEmpty(sample.Tags);
    }

    [TestMethod]
    public async Task EnsureLoadedAsync_FaultedLoad_ReturnsInFlightToBaseline()
    {
        var loader = Substitute.For<IMapRegionLoader>();
        loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("boom"));
        using var provider = new ServiceCollection().AddScoped(_ => loader).BuildServiceProvider();
        using var scheduler = CreateScheduler(provider, new ManualTimeProvider());
        using var spy = new MeterListenerSpy(MeterName);
        var baseline = spy.ObserveGauges()[InFlight];

        await scheduler.StartAsync(CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(() => scheduler.EnsureLoadedAsync([CreateRegion(1, () => false)]));
        await scheduler.StopAsync(CancellationToken.None);

        Assert.AreEqual(baseline, spy.ObserveGauges()[InFlight]);
    }

    [TestMethod]
    public async Task StopAsync_WithQueuedRequests_ReturnsInFlightToBaseline()
    {
        var loader = Substitute.For<IMapRegionLoader>();
        using var provider = new ServiceCollection().AddScoped(_ => loader).BuildServiceProvider();
        using var scheduler = CreateScheduler(provider, new ManualTimeProvider());
        using var spy = new MeterListenerSpy(MeterName);
        var baseline = spy.ObserveGauges()[InFlight];

        // The scheduler is never started, so both requests stay queued until shutdown abandons them.
        var wait = scheduler.EnsureLoadedAsync([CreateRegion(1, () => false), CreateRegion(2, () => false)]);
        Assert.AreEqual(baseline + 2, spy.ObserveGauges()[InFlight]);

        await scheduler.StopAsync(CancellationToken.None);
        await Assert.ThrowsAsync<OperationCanceledException>(() => wait);

        Assert.AreEqual(baseline, spy.ObserveGauges()[InFlight]);
        Assert.IsEmpty(spy.Measurements.Where(item => item.InstrumentName == QueueDuration).ToList());
    }

    private static MapRegionLoadScheduler CreateScheduler(ServiceProvider provider, TimeProvider time) =>
        new(provider.GetRequiredService<IServiceScopeFactory>(), Substitute.For<ILogger<MapRegionLoadScheduler>>(), time);

    private static IMapRegion CreateRegion(int id, Func<bool> isReady)
    {
        var region = Substitute.For<IMapRegion>();
        region.Id.Returns(id);
        region.BaseLocation.Returns(Location.Create(id << 6, 0, 0, 0));
        region.State.Returns(_ => isReady() ? MapRegionState.Ready : MapRegionState.Initializing);
        return region;
    }

    private sealed class StubTask : ITaskItem
    {
        private int _ticks;

        public int TicksToComplete { get; init; } = int.MaxValue;
        public Action? OnTick { get; init; }
        public bool IsCancelled => false;
        public bool IsCompleted => _ticks >= TicksToComplete;
        public bool IsFaulted => false;

        public void Tick()
        {
            _ticks++;
            OnTick?.Invoke();
        }

        public void Cancel() { }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp = 1; // Non-zero: zero marks "not sampled" in the scheduler.

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => Interlocked.Read(ref _timestamp);

        public void Advance(TimeSpan amount) => Interlocked.Add(ref _timestamp, amount.Ticks);
    }
}
