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
public sealed class GameWorkerServiceTests
{
    private static readonly IReadOnlyDictionary<int, ICharacter> EmptyCharacters = new Dictionary<int, ICharacter>();

    [TestMethod]
    public async Task ExecuteTickAsync_CompletesBeforeTheNextInvocationStarts()
    {
        var firstStarted = NewSignal();
        using var firstRelease = new ManualResetEventSlim();
        var secondStarted = NewSignal();
        var majorUpdateCalls = 0;
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ =>
        {
            if (Interlocked.Increment(ref majorUpdateCalls) == 1)
            {
                firstStarted.TrySetResult();
                firstRelease.Wait();
                return;
            }

            secondStarted.TrySetResult();
        });

        using var worker = CreateWorker(region, TimeSpan.Zero).Worker;
        var firstTick = Task.Run(() => worker.ExecuteTickAsync(CancellationToken.None));
        try
        {
            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.AreEqual(1, Volatile.Read(ref majorUpdateCalls));
            Assert.IsFalse(secondStarted.Task.IsCompleted);

            firstRelease.Set();
            await firstTick.WaitAsync(TimeSpan.FromSeconds(1));
            await worker.ExecuteTickAsync(CancellationToken.None);
            await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.AreEqual(2, Volatile.Read(ref majorUpdateCalls));
        }
        finally
        {
            firstRelease.Set();
            await firstTick.WaitAsync(TimeSpan.FromSeconds(1));
        }
    }

    [TestMethod]
    public async Task ExecuteTickAsync_AdjacentTicksPreservePhaseOrder()
    {
        var events = new List<string>();
        var currentTick = 0;
        var regionOne = Substitute.For<IMapRegion>();
        var regionTwo = Substitute.For<IMapRegion>();
        ConfigureRegion(regionOne, "one", events, () =>
        {
            var tick = Interlocked.Increment(ref currentTick);
            events.Add($"major-{tick}-one");
        }, () => Volatile.Read(ref currentTick));
        ConfigureRegion(regionTwo, "two", events, () => events.Add($"major-{Volatile.Read(ref currentTick)}-two"), () => Volatile.Read(ref currentTick));

        using var worker = CreateWorker(new[] { regionOne, regionTwo }, TimeSpan.Zero).Worker;
        await worker.ExecuteTickAsync(CancellationToken.None);
        await worker.ExecuteTickAsync(CancellationToken.None);

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

        CollectionAssert.AreEqual(expectedEvents, events);
    }

    [TestMethod]
    public async Task HostedLoop_DoesNotOverlapTicks()
    {
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

        using var worker = CreateWorker(region, TimeSpan.FromMilliseconds(10)).Worker;
        var startAttempted = false;
        try
        {
            startAttempted = true;
            await worker.StartAsync(CancellationToken.None);
            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            // Five configured intervals give a concurrently dispatched tick a bounded opportunity to start.
            await Task.Delay(TimeSpan.FromMilliseconds(50));

            Assert.AreEqual(1, Volatile.Read(ref majorUpdateCalls));
            Assert.IsFalse(secondStarted.Task.IsCompleted);

            firstRelease.Set();
            await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.AreEqual(2, Volatile.Read(ref majorUpdateCalls));
        }
        finally
        {
            firstRelease.Set();
            secondRelease.Set();
            if (startAttempted && worker.ExecuteTask is { IsCompleted: false })
            {
                await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
            }
        }

        Assert.IsTrue(worker.ExecuteTask?.IsCompleted ?? false);
    }

    [TestMethod]
    public async Task StopAsync_WaitsForTheOwnedTickBeforeCompleting()
    {
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

        using var worker = CreateWorker(region, TimeSpan.FromMilliseconds(1)).Worker;
        try
        {
            await worker.StartAsync(CancellationToken.None);
            await tickStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            var stopTask = worker.StopAsync(CancellationToken.None);
            Assert.IsFalse(stopTask.IsCompleted);
            Assert.AreEqual(0, Volatile.Read(ref prepareCalls));

            tickRelease.Set();
            await stopTask.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.IsTrue(worker.ExecuteTask?.IsCompleted ?? false);
        }
        finally
        {
            tickRelease.Set();
            if (worker.ExecuteTask is { IsCompleted: false })
            {
                await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
            }
        }

        Assert.AreEqual(1, Volatile.Read(ref prepareCalls));
        Assert.AreEqual(1, Volatile.Read(ref updateCalls));
        Assert.AreEqual(1, Volatile.Read(ref resetCalls));
    }

    [TestMethod]
    public async Task ExecuteTickAsync_CancellationBeforeTickLeavesRegionsUntouched()
    {
        var regionService = Substitute.For<IMapRegionService>();
        using var worker = CreateWorker(regionService, TimeSpan.Zero).Worker;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => worker.ExecuteTickAsync(cancellation.Token));

        regionService.DidNotReceive().FindAllRegions();
    }

    [TestMethod]
    public async Task ExecuteTickAsync_PropagatesUnexpectedExceptionToHostedLoopOwner()
    {
        var failure = new InvalidOperationException("tick failure");
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ => throw failure);
        var logger = new TestLogger<GameWorkerService>();
        using var worker = CreateWorker(region, TimeSpan.Zero, logger: logger).Worker;

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => worker.ExecuteTickAsync(CancellationToken.None));

        Assert.AreSame(failure, actual);
        Assert.AreEqual(0, logger.ErrorCount);
    }

    [TestMethod]
    public async Task ExecuteTickAsync_PropagatesForeignCancellationToHostedLoopOwner()
    {
        using var unrelatedCancellation = new CancellationTokenSource();
        var failure = new OperationCanceledException("tick cancellation was not worker cancellation", unrelatedCancellation.Token);
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ => throw failure);
        using var worker = CreateWorker(region, TimeSpan.Zero).Worker;

        var actual = await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => worker.ExecuteTickAsync(CancellationToken.None));

        Assert.AreSame(failure, actual);
    }

    [TestMethod]
    public async Task HostedLoop_LogsUnexpectedTickExceptionAndContinues()
    {
        var firstStarted = NewSignal();
        var secondStarted = NewSignal();
        using var secondRelease = new ManualResetEventSlim();
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

            secondStarted.TrySetResult();
            secondRelease.Wait();
        });

        using var worker = CreateWorker(region, TimeSpan.FromMilliseconds(1), logger: logger).Worker;
        try
        {
            await worker.StartAsync(CancellationToken.None);
            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.AreEqual(1, logger.ErrorCount);
            Assert.AreSame(failure, logger.LastError!.Exception);
        }
        finally
        {
            secondRelease.Set();
            if (worker.ExecuteTask is { IsCompleted: false })
            {
                await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
            }
        }
    }

    [TestMethod]
    public async Task HostedLoop_LogsForeignCancellationAndContinues()
    {
        var firstStarted = NewSignal();
        var secondStarted = NewSignal();
        using var secondRelease = new ManualResetEventSlim();
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

            secondStarted.TrySetResult();
            secondRelease.Wait();
        });

        using var worker = CreateWorker(region, TimeSpan.FromMilliseconds(1), logger: logger).Worker;
        try
        {
            await worker.StartAsync(CancellationToken.None);
            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.AreEqual(1, logger.ErrorCount);
            Assert.AreSame(failure, logger.LastError!.Exception);
        }
        finally
        {
            secondRelease.Set();
            if (worker.ExecuteTask is { IsCompleted: false })
            {
                await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
            }
        }
    }

    [TestMethod]
    public async Task ExecuteTickAsync_LogsOverrunAfterTheWholeTickCompletes()
    {
        var tickStarted = NewSignal();
        using var tickRelease = new ManualResetEventSlim();
        var logger = new TestLogger<GameWorkerService>();
        var region = Substitute.For<IMapRegion>();
        region.When(item => item.MajorUpdateTick()).Do(_ =>
        {
            tickStarted.TrySetResult();
            tickRelease.Wait();
        });

        using var worker = CreateWorker(region, TimeSpan.Zero, logger: logger).Worker;
        var tick = Task.Run(() => worker.ExecuteTickAsync(CancellationToken.None));
        try
        {
            await tickStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.AreEqual(0, logger.WarningCount);

            tickRelease.Set();
            await tick.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.AreEqual(1, logger.WarningCount);
            Assert.IsTrue(logger.LastWarning!.Message.Contains("exceeded its configured budget", StringComparison.Ordinal));
        }
        finally
        {
            tickRelease.Set();
            await tick.WaitAsync(TimeSpan.FromSeconds(1));
        }
    }

    [TestMethod]
    public async Task ExecuteTickAsync_CapturesOneSnapshotAndSharesItAcrossRegions()
    {
        var snapshot = new Dictionary<int, ICharacter>();
        var snapshotCalls = 0;
        var updateSnapshots = new List<IReadOnlyDictionary<int, ICharacter>>();
        var regionOne = Substitute.For<IMapRegion>();
        var regionTwo = Substitute.For<IMapRegion>();
        regionOne.When(item => item.MajorClientUpdateTick(Arg.Any<IReadOnlyDictionary<int, ICharacter>>())).Do(call => updateSnapshots.Add(call.Arg<IReadOnlyDictionary<int, ICharacter>>()!));
        regionTwo.When(item => item.MajorClientUpdateTick(Arg.Any<IReadOnlyDictionary<int, ICharacter>>())).Do(call => updateSnapshots.Add(call.Arg<IReadOnlyDictionary<int, ICharacter>>()!));
        var store = Substitute.For<ICharacterStore>();
#pragma warning disable CA2012 // NSubstitute consumes the configured ValueTask exactly once.
        store.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Interlocked.Increment(ref snapshotCalls);
            return new ValueTask<IReadOnlyDictionary<int, ICharacter>>(snapshot);
        });
#pragma warning restore CA2012

        using var worker = CreateWorker(new[] { regionOne, regionTwo }, TimeSpan.Zero, store).Worker;
        await worker.ExecuteTickAsync(CancellationToken.None);
        await worker.ExecuteTickAsync(CancellationToken.None);

        Assert.AreEqual(2, Volatile.Read(ref snapshotCalls));
        Assert.HasCount(4, updateSnapshots);
        Assert.IsTrue(updateSnapshots.All(item => ReferenceEquals(snapshot, item)));
    }

    [TestMethod]
    public async Task HostCancellation_ExitsWithoutUnexpectedFailureLog()
    {
        var logger = new TestLogger<GameWorkerService>();
        var regionService = Substitute.For<IMapRegionService>();
        using var worker = CreateWorker(regionService, TimeSpan.FromDays(1), logger: logger).Worker;

        var started = false;
        try
        {
            await worker.StartAsync(CancellationToken.None);
            started = true;
            await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));

            Assert.AreEqual(0, logger.ErrorCount);
            Assert.IsTrue(worker.ExecuteTask?.IsCompleted ?? false);
            regionService.DidNotReceive().FindAllRegions();
        }
        finally
        {
            if (started && worker.ExecuteTask is { IsCompleted: false })
            {
                await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
            }
        }
    }

    private static (GameWorkerService Worker, ICharacterStore Store) CreateWorker(
        IMapRegion region,
        TimeSpan tickTimeSpan,
        TestLogger<GameWorkerService>? logger = null) =>
        CreateWorker(new[] { region }, tickTimeSpan, CreateCharacterStore(), logger);

    private static (GameWorkerService Worker, ICharacterStore Store) CreateWorker(
        IEnumerable<IMapRegion> regions,
        TimeSpan tickTimeSpan,
        TestLogger<GameWorkerService>? logger = null) =>
        CreateWorker(regions, tickTimeSpan, CreateCharacterStore(), logger);

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

    private static ICharacterStore CreateCharacterStore()
    {
        var store = Substitute.For<ICharacterStore>();
#pragma warning disable CA2012 // NSubstitute consumes the configured ValueTask exactly once.
        store.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(_ => new ValueTask<IReadOnlyDictionary<int, ICharacter>>(EmptyCharacters));
#pragma warning restore CA2012
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

    private static TaskCompletionSource NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class TestLogger<T> : ILogger<T>
    {
        private int _warningCount;
        private int _errorCount;

        public int WarningCount => Volatile.Read(ref _warningCount);

        public int ErrorCount => Volatile.Read(ref _errorCount);

        public LogEntry? LastWarning { get; private set; }

        public LogEntry? LastError { get; private set; }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel < LogLevel.Warning)
            {
                return;
            }

            var entry = new LogEntry(logLevel, formatter(state, exception), exception);
            if (logLevel >= LogLevel.Error)
            {
                Interlocked.Increment(ref _errorCount);
                LastError = entry;
            }
            else
            {
                Interlocked.Increment(ref _warningCount);
                LastWarning = entry;
            }
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
