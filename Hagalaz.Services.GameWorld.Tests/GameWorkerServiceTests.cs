using System;
using System.Collections.Concurrent;
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
public sealed class GameWorkerServiceTests
{
    private static readonly IReadOnlyDictionary<int, ICharacter> EmptyCharacters = new Dictionary<int, ICharacter>();

    [TestMethod]
    public async Task OverrunningMajorTick_DoesNotStartAnotherMajorTickBeforeFirstCompletes()
    {
        var snapshotGate = NewSnapshotGate();
        var firstStarted = NewSignal();
        using var firstRelease = new ManualResetEventSlim();
        var secondStarted = NewSignal();
        using var secondRelease = new ManualResetEventSlim();
        var majorUpdateCalls = 0;
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ =>
        {
            var call = Interlocked.Increment(ref majorUpdateCalls);
            if (call == 1)
            {
                firstStarted.TrySetResult();
                firstRelease.Wait();
                return;
            }

            secondStarted.TrySetResult();
            secondRelease.Wait();
        });

        var (worker, _) = CreateWorker(region, TimeSpan.Zero, snapshotGate);
        await worker.StartAsync(CancellationToken.None);
        snapshotGate.TrySetResult(EmptyCharacters);
        await firstStarted.Task;

        Assert.AreEqual(1, Volatile.Read(ref majorUpdateCalls));
        Assert.IsFalse(secondStarted.Task.IsCompleted);

        firstRelease.Set();
        await secondStarted.Task;

        var stopTask = worker.StopAsync(CancellationToken.None);
        try
        {
            Assert.IsFalse(stopTask.IsCompleted);
        }
        finally
        {
            secondRelease.Set();
            await stopTask;
            worker.Dispose();
        }
    }

    [TestMethod]
    public async Task AdjacentTicks_PreservePhaseOrderAndDoNotOverlap()
    {
        var snapshotGate = NewSnapshotGate();
        var events = new List<string>();
        var secondMajorStarted = NewSignal();
        using var secondMajorRelease = new ManualResetEventSlim();
        var currentTick = 0;
        var regionOne = Substitute.For<IMapRegion>();
        var regionTwo = Substitute.For<IMapRegion>();

        ConfigureRegion(regionOne, "one", events, () =>
        {
            var tick = Interlocked.Increment(ref currentTick);
            events.Add($"major-{tick}-one");
            if (tick == 2)
            {
                secondMajorStarted.TrySetResult();
                secondMajorRelease.Wait();
            }
        }, () => Volatile.Read(ref currentTick));
        ConfigureRegion(regionTwo, "two", events, () => events.Add($"major-{Volatile.Read(ref currentTick)}-two"), () => Volatile.Read(ref currentTick));

        var (worker, _) = CreateWorker(new[] { regionOne, regionTwo }, TimeSpan.Zero, snapshotGate);
        await worker.StartAsync(CancellationToken.None);
        snapshotGate.TrySetResult(EmptyCharacters);
        await secondMajorStarted.Task;

        var stopTask = worker.StopAsync(CancellationToken.None);
        try
        {
            Assert.IsFalse(stopTask.IsCompleted);
        }
        finally
        {
            secondMajorRelease.Set();
            await stopTask;
            worker.Dispose();
        }

        var expectedEvents = new[]
        {
            "major-1-one", "major-1-two",
            "prepare-1-one", "prepare-1-two",
            "update-1-one", "update-1-two",
            "reset-1-one", "reset-1-two",
            "major-2-one", "major-2-two",
            "prepare-2-one", "prepare-2-two",
            "update-2-one", "update-2-two",
            "reset-2-one", "reset-2-two"
        };
        Assert.IsTrue(events.SequenceEqual(expectedEvents), string.Join("|", events));
    }

    [TestMethod]
    public async Task StopAsync_WaitsForTheOwnedTickBeforeCompleting()
    {
        var snapshotGate = NewSnapshotGate();
        var tickStarted = NewSignal();
        using var tickRelease = new ManualResetEventSlim();
        var prepareCalls = 0;
        var updateCalls = 0;
        var resetCalls = 0;
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ =>
        {
            tickStarted.TrySetResult();
            tickRelease.Wait();
        });
        region.When(item => item.MajorClientPrepareUpdateTick()).Do(_ => Interlocked.Increment(ref prepareCalls));
        region.When(item => item.MajorClientUpdateTick(Arg.Any<IReadOnlyDictionary<int, ICharacter>>())).Do(_ => Interlocked.Increment(ref updateCalls));
        region.When(item => item.MajorClientUpdateResetTick()).Do(_ => Interlocked.Increment(ref resetCalls));

        var (worker, _) = CreateWorker(region, TimeSpan.Zero, snapshotGate);
        await worker.StartAsync(CancellationToken.None);
        snapshotGate.TrySetResult(EmptyCharacters);
        await tickStarted.Task;

        var stopTask = worker.StopAsync(CancellationToken.None);
        try
        {
            Assert.IsFalse(stopTask.IsCompleted);
            Assert.AreEqual(0, Volatile.Read(ref prepareCalls));
        }
        finally
        {
            tickRelease.Set();
            await stopTask;
            worker.Dispose();
        }

        Assert.AreEqual(1, Volatile.Read(ref prepareCalls));
        Assert.AreEqual(1, Volatile.Read(ref updateCalls));
        Assert.AreEqual(1, Volatile.Read(ref resetCalls));
    }

    [TestMethod]
    public async Task StopAsync_ReportsTimeoutWhenHostTokenExpiresBeforeSynchronousTickFinishes()
    {
        var snapshotGate = NewSnapshotGate();
        var tickStarted = NewSignal();
        using var tickRelease = new ManualResetEventSlim();
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ =>
        {
            tickStarted.TrySetResult();
            tickRelease.Wait();
        });

        var (worker, _) = CreateWorker(region, TimeSpan.Zero, snapshotGate);
        await worker.StartAsync(CancellationToken.None);
        snapshotGate.TrySetResult(EmptyCharacters);
        await tickStarted.Task;

        using var hostShutdown = new CancellationTokenSource();
        var stopTask = worker.StopAsync(hostShutdown.Token);
        hostShutdown.Cancel();

        try
        {
            await Assert.ThrowsExactlyAsync<TimeoutException>(() => stopTask);
            Assert.IsFalse(worker.ExecuteTask!.IsCompleted);
        }
        finally
        {
            tickRelease.Set();
            await worker.StopAsync(CancellationToken.None);
            worker.Dispose();
        }
    }

    [TestMethod]
    public async Task Overrun_IsLoggedAfterTheWholeTickCompletes()
    {
        var snapshotGate = NewSnapshotGate();
        var firstTickStarted = NewSignal();
        using var firstTickRelease = new ManualResetEventSlim();
        var secondTickStarted = NewSignal();
        using var secondTickRelease = new ManualResetEventSlim();
        var majorUpdateCalls = 0;
        var logger = new TestLogger<GameWorkerService>();
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ =>
        {
            if (Interlocked.Increment(ref majorUpdateCalls) == 1)
            {
                firstTickStarted.TrySetResult();
                firstTickRelease.Wait();
                return;
            }

            secondTickStarted.TrySetResult();
            secondTickRelease.Wait();
        });

        var (worker, _) = CreateWorker(region, TimeSpan.Zero, snapshotGate, logger);
        await worker.StartAsync(CancellationToken.None);
        snapshotGate.TrySetResult(EmptyCharacters);
        await firstTickStarted.Task;
        firstTickRelease.Set();
        await secondTickStarted.Task;

        var stopTask = worker.StopAsync(CancellationToken.None);
        try
        {
            Assert.IsTrue(logger.Entries.Any(entry => entry.Level == LogLevel.Warning));
        }
        finally
        {
            secondTickRelease.Set();
            await stopTask;
            worker.Dispose();
        }

        Assert.IsTrue(logger.Entries.Any(entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("exceeded its configured budget", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task UnexpectedTickException_IsLoggedAndLoopCanProceed()
    {
        var snapshotGate = NewSnapshotGate();
        var firstStarted = NewSignal();
        var secondMajorStarted = NewSignal();
        using var secondMajorRelease = new ManualResetEventSlim();
        var majorUpdateCalls = 0;
        var logger = new TestLogger<GameWorkerService>();
        var failure = new InvalidOperationException("tick failure");
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ =>
        {
            if (Interlocked.Increment(ref majorUpdateCalls) == 1)
            {
                firstStarted.TrySetResult();
                throw failure;
            }

            secondMajorStarted.TrySetResult();
            secondMajorRelease.Wait();
        });

        var (worker, _) = CreateWorker(region, TimeSpan.Zero, snapshotGate, logger);
        await worker.StartAsync(CancellationToken.None);
        snapshotGate.TrySetResult(EmptyCharacters);
        await firstStarted.Task;
        await secondMajorStarted.Task;

        var stopTask = worker.StopAsync(CancellationToken.None);
        try
        {
            Assert.IsTrue(logger.Entries.Any(entry => entry.Level == LogLevel.Error && ReferenceEquals(entry.Exception, failure)));
        }
        finally
        {
            secondMajorRelease.Set();
            await stopTask;
            worker.Dispose();
        }
    }

    [TestMethod]
    public async Task TickOperationCanceledException_DuringShutdown_IsLoggedAsTickFailure()
    {
        var snapshotGate = NewSnapshotGate();
        var firstStarted = NewSignal();
        var secondMajorStarted = NewSignal();
        using var secondMajorRelease = new ManualResetEventSlim();
        using var unrelatedCancellation = new CancellationTokenSource();
        var majorUpdateCalls = 0;
        var logger = new TestLogger<GameWorkerService>();
        var failure = new OperationCanceledException("tick cancellation was not worker cancellation", unrelatedCancellation.Token);
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ =>
        {
            if (Interlocked.Increment(ref majorUpdateCalls) == 1)
            {
                firstStarted.TrySetResult();
                throw failure;
            }

            secondMajorStarted.TrySetResult();
            secondMajorRelease.Wait();
        });

        var (worker, _) = CreateWorker(region, TimeSpan.Zero, snapshotGate, logger);
        await worker.StartAsync(CancellationToken.None);
        snapshotGate.TrySetResult(EmptyCharacters);
        await firstStarted.Task;
        await secondMajorStarted.Task;

        var stopTask = worker.StopAsync(CancellationToken.None);
        try
        {
            Assert.IsTrue(logger.Entries.Any(entry => entry.Level == LogLevel.Error && ReferenceEquals(entry.Exception, failure)));
        }
        finally
        {
            secondMajorRelease.Set();
            await stopTask;
            worker.Dispose();
        }
    }

    [TestMethod]
    public async Task CharacterSnapshot_IsCapturedOnceAndSharedAcrossRegions()
    {
        var snapshotGate = NewSnapshotGate();
        var snapshot = new Dictionary<int, ICharacter>();
        var snapshotCalls = 0;
        var updateSnapshots = new List<IReadOnlyDictionary<int, ICharacter>>();
        var regionOne = Substitute.For<IMapRegion>();
        var regionTwo = Substitute.For<IMapRegion>();
        var secondMajorStarted = NewSignal();
        using var secondMajorRelease = new ManualResetEventSlim();
        var regionOneMajorCalls = 0;

        var store = Substitute.For<ICharacterStore>();
        store.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref snapshotCalls) == 1)
            {
                return new ValueTask<IReadOnlyDictionary<int, ICharacter>>(snapshotGate.Task);
            }

            return new ValueTask<IReadOnlyDictionary<int, ICharacter>>(snapshot);
        });

        regionOne.When(item => item.MajorUpdateTick()).Do(_ =>
        {
            if (Interlocked.Increment(ref regionOneMajorCalls) == 2)
            {
                secondMajorStarted.TrySetResult();
                secondMajorRelease.Wait();
            }
        });
        regionOne.When(item => item.MajorClientUpdateTick(Arg.Any<IReadOnlyDictionary<int, ICharacter>>())).Do(call => updateSnapshots.Add(call.Arg<IReadOnlyDictionary<int, ICharacter>>()));
        regionTwo.When(item => item.MajorClientUpdateTick(Arg.Any<IReadOnlyDictionary<int, ICharacter>>())).Do(call => updateSnapshots.Add(call.Arg<IReadOnlyDictionary<int, ICharacter>>()));

        var (worker, _) = CreateWorker(new[] { regionOne, regionTwo }, TimeSpan.Zero, store);
        await worker.StartAsync(CancellationToken.None);
        snapshotGate.TrySetResult(snapshot);
        await secondMajorStarted.Task;

        var stopTask = worker.StopAsync(CancellationToken.None);
        try
        {
            Assert.AreEqual(2, Volatile.Read(ref snapshotCalls));
            Assert.HasCount(2, updateSnapshots);
            Assert.IsTrue(ReferenceEquals(snapshot, updateSnapshots[0]));
            Assert.IsTrue(ReferenceEquals(snapshot, updateSnapshots[1]));
        }
        finally
        {
            secondMajorRelease.Set();
            await stopTask;
            worker.Dispose();
        }
    }

    [TestMethod]
    public async Task HostCancellation_ExitsWithoutUnexpectedFailureLog()
    {
        var logger = new TestLogger<GameWorkerService>();
        var regionService = Substitute.For<IMapRegionService>();
        var (worker, _) = CreateWorker(regionService, TimeSpan.FromDays(1), logger: logger);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);
        worker.Dispose();

        Assert.IsFalse(logger.Entries.Any(entry => entry.Level == LogLevel.Error));
        regionService.DidNotReceive().FindAllRegions();
    }

    private static (GameWorkerService Worker, ICharacterStore Store) CreateWorker(
        IMapRegion region,
        TimeSpan tickTimeSpan,
        TaskCompletionSource<IReadOnlyDictionary<int, ICharacter>>? snapshotGate = null,
        TestLogger<GameWorkerService>? logger = null) =>
        CreateWorker(new[] { region }, tickTimeSpan, snapshotGate, logger);

    private static (GameWorkerService Worker, ICharacterStore Store) CreateWorker(
        IEnumerable<IMapRegion> regions,
        TimeSpan tickTimeSpan,
        TaskCompletionSource<IReadOnlyDictionary<int, ICharacter>>? snapshotGate = null,
        TestLogger<GameWorkerService>? logger = null) =>
        CreateWorker(new[] { regions }, tickTimeSpan, snapshotGate, logger);

    private static (GameWorkerService Worker, ICharacterStore Store) CreateWorker(
        IEnumerable<IMapRegion> regionSet,
        TimeSpan tickTimeSpan,
        ICharacterStore characterStore,
        TestLogger<GameWorkerService>? logger = null)
    {
        var regionService = Substitute.For<IMapRegionService>();
        regionService.FindAllRegions().Returns(regionSet);
        return CreateWorker(regionService, tickTimeSpan, characterStore, logger);
    }

    private static (GameWorkerService Worker, ICharacterStore Store) CreateWorker(
        IMapRegionService regionService,
        TimeSpan tickTimeSpan,
        ICharacterStore? characterStore = null,
        TestLogger<GameWorkerService>? logger = null)
    {
        var scheduler = Substitute.For<IRsTaskService>();
        characterStore ??= CreateCharacterStore();
        var worker = new GameWorkerService(
            scheduler,
            regionService,
            characterStore,
            Options.Create(new GameServerOptions
            {
                AuthenticationToken = string.Empty,
                ClientRevision = 0,
                ClientRevisionPatch = 0,
                TickTimeSpan = tickTimeSpan
            }),
            logger ?? new TestLogger<GameWorkerService>());
        return (worker, characterStore);
    }

    private static (GameWorkerService Worker, ICharacterStore Store) CreateWorker(
        IEnumerable<IEnumerable<IMapRegion>> regionSets,
        TimeSpan tickTimeSpan,
        TaskCompletionSource<IReadOnlyDictionary<int, ICharacter>>? snapshotGate,
        TestLogger<GameWorkerService>? logger)
    {
        var regionService = Substitute.For<IMapRegionService>();
        regionService.FindAllRegions().Returns(regionSets.SelectMany(regions => regions));
        var store = CreateCharacterStore(snapshotGate);
        return CreateWorker(regionService, tickTimeSpan, store, logger);
    }

    private static ICharacterStore CreateCharacterStore(
        TaskCompletionSource<IReadOnlyDictionary<int, ICharacter>>? snapshotGate = null)
    {
        var store = Substitute.For<ICharacterStore>();
        if (snapshotGate is null)
        {
            store.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<IReadOnlyDictionary<int, ICharacter>>(EmptyCharacters));
        }
        else
        {
            store.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<IReadOnlyDictionary<int, ICharacter>>(snapshotGate.Task));
        }

        return store;
    }

    private static void ConfigureRegion(
        IMapRegion region,
        string name,
        ICollection<string> events,
        Action majorUpdate,
        Func<int> currentTick)
    {
        region.When(item => item.MajorUpdateTick()).Do(_ => majorUpdate());
        region.When(item => item.MajorClientPrepareUpdateTick()).Do(_ => events.Add($"prepare-{currentTick()}-{name}"));
        region.When(item => item.MajorClientUpdateTick(Arg.Any<IReadOnlyDictionary<int, ICharacter>>())).Do(_ => events.Add($"update-{currentTick()}-{name}"));
        region.When(item => item.MajorClientUpdateResetTick()).Do(_ => events.Add($"reset-{currentTick()}-{name}"));
    }

    private static TaskCompletionSource<IReadOnlyDictionary<int, ICharacter>> NewSnapshotGate() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class TestLogger<T> : ILogger<T>
    {
        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
