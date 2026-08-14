using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class GameWorkerServiceTests
{
    [TestMethod]
    public async Task OverrunningMajorTick_DoesNotStartAnotherMajorTickBeforeFirstCompletes()
    {
        var firstStarted = NewSignal();
        var firstRelease = NewSignal();
        var secondStarted = NewSignal();
        var majorUpdateCalls = 0;
        var region = Substitute.For<IMapRegion>();
        region.MajorUpdateTick().Returns(_ =>
        {
            var call = Interlocked.Increment(ref majorUpdateCalls);
            if (call == 1)
            {
                firstStarted.TrySetResult();
                return firstRelease.Task;
            }

            secondStarted.TrySetResult();
            return Task.CompletedTask;
        });

        var logger = new TestLogger<GameWorkerService>();
        var worker = CreateWorker(region, TimeSpan.Zero, logger);
        await worker.StartAsync(CancellationToken.None);
        await firstStarted.Task;

        try
        {
            Assert.AreEqual(1, Volatile.Read(ref majorUpdateCalls));
            Assert.IsFalse(secondStarted.Task.IsCompleted);
        }
        finally
        {
            firstRelease.TrySetResult();
            await worker.StopAsync(CancellationToken.None);
            worker.Dispose();
        }
    }

    [TestMethod]
    public async Task AdjacentTicks_PreservePhaseOrderAndDoNotOverlap()
    {
        var events = new List<string>();
        var secondMajorStarted = NewSignal();
        var secondMajorRelease = NewSignal();
        var secondRegionMajorStarted = NewSignal();
        var secondRegionMajorRelease = NewSignal();
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
                return secondMajorRelease.Task;
            }

            return Task.CompletedTask;
        }, () => Volatile.Read(ref currentTick));
        ConfigureRegion(regionTwo, "two", events, () =>
        {
            var tick = Volatile.Read(ref currentTick);
            events.Add($"major-{tick}-two");
            if (tick == 2)
            {
                secondRegionMajorStarted.TrySetResult();
                return secondRegionMajorRelease.Task;
            }

            return Task.CompletedTask;
        }, () => Volatile.Read(ref currentTick));

        var worker = CreateWorker(new[] { regionOne, regionTwo }, TimeSpan.Zero);
        await worker.StartAsync(CancellationToken.None);
        await secondMajorStarted.Task;

        Assert.IsFalse(secondRegionMajorStarted.Task.IsCompleted);
        secondMajorRelease.TrySetResult();
        await secondRegionMajorStarted.Task;

        var stopTask = worker.StopAsync(CancellationToken.None);
        try
        {
            Assert.IsTrue(events.SequenceEqual(new[]
            {
                "major-1-one", "major-1-two",
                "prepare-1-one", "prepare-1-two",
                "update-1-one", "update-1-two",
                "reset-1-one", "reset-1-two",
                "major-2-one", "major-2-two"
            }), string.Join("|", events));
            Assert.IsFalse(stopTask.IsCompleted);
        }
        finally
        {
            secondRegionMajorRelease.TrySetResult();
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
        var tickStarted = NewSignal();
        var tickRelease = NewSignal();
        var prepareCalls = 0;
        var updateCalls = 0;
        var resetCalls = 0;
        var region = Substitute.For<IMapRegion>();
        region.MajorUpdateTick().Returns(_ =>
        {
            tickStarted.TrySetResult();
            return tickRelease.Task;
        });
        region.MajorClientPrepareUpdateTick().Returns(_ =>
        {
            Interlocked.Increment(ref prepareCalls);
            return Task.CompletedTask;
        });
        region.MajorClientUpdateTick().Returns(_ =>
        {
            Interlocked.Increment(ref updateCalls);
            return Task.CompletedTask;
        });
        region.MajorClientUpdateResetTick().Returns(_ =>
        {
            Interlocked.Increment(ref resetCalls);
            return Task.CompletedTask;
        });

        var worker = CreateWorker(region, TimeSpan.Zero);
        await worker.StartAsync(CancellationToken.None);
        await tickStarted.Task;

        var stopTask = worker.StopAsync(CancellationToken.None);
        try
        {
            Assert.IsFalse(stopTask.IsCompleted);
            Assert.AreEqual(0, Volatile.Read(ref prepareCalls));
        }
        finally
        {
            tickRelease.TrySetResult();
            await stopTask;
            worker.Dispose();
        }

        Assert.AreEqual(1, Volatile.Read(ref prepareCalls));
        Assert.AreEqual(1, Volatile.Read(ref updateCalls));
        Assert.AreEqual(1, Volatile.Read(ref resetCalls));
    }

    [TestMethod]
    public async Task Overrun_IsLoggedAfterTheWholeTickCompletes()
    {
        var tickStarted = NewSignal();
        var tickRelease = NewSignal();
        var logger = new TestLogger<GameWorkerService>();
        var region = Substitute.For<IMapRegion>();
        region.MajorUpdateTick().Returns(_ =>
        {
            tickStarted.TrySetResult();
            return tickRelease.Task;
        });

        var worker = CreateWorker(region, TimeSpan.Zero, logger);
        await worker.StartAsync(CancellationToken.None);
        await tickStarted.Task;

        var stopTask = worker.StopAsync(CancellationToken.None);
        try
        {
            Assert.IsFalse(logger.Entries.Any(entry => entry.Level == LogLevel.Warning));
        }
        finally
        {
            tickRelease.TrySetResult();
            await stopTask;
            worker.Dispose();
        }

        Assert.IsTrue(logger.Entries.Any(entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("exceeded its configured budget", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task UnexpectedTickException_IsLoggedAndLoopCanProceed()
    {
        var firstStarted = NewSignal();
        var firstFailure = NewSignal();
        var secondMajorStarted = NewSignal();
        var secondMajorRelease = NewSignal();
        var majorUpdateCalls = 0;
        var failure = new InvalidOperationException("tick failure");
        var logger = new TestLogger<GameWorkerService>();
        var region = Substitute.For<IMapRegion>();
        region.MajorUpdateTick().Returns(_ =>
        {
            if (Interlocked.Increment(ref majorUpdateCalls) == 1)
            {
                firstStarted.TrySetResult();
                return firstFailure.Task;
            }

            secondMajorStarted.TrySetResult();
            return secondMajorRelease.Task;
        });

        var worker = CreateWorker(region, TimeSpan.Zero, logger);
        await worker.StartAsync(CancellationToken.None);
        await firstStarted.Task;
        Assert.IsFalse(secondMajorStarted.Task.IsCompleted);
        firstFailure.TrySetException(failure);
        await secondMajorStarted.Task;

        var stopTask = worker.StopAsync(CancellationToken.None);
        try
        {
            Assert.IsTrue(logger.Entries.Any(entry => entry.Level == LogLevel.Error && ReferenceEquals(entry.Exception, failure)));
        }
        finally
        {
            secondMajorRelease.TrySetResult();
            await stopTask;
            worker.Dispose();
        }
    }

    [TestMethod]
    public async Task HostCancellation_ExitsWithoutUnexpectedFailureLog()
    {
        var logger = new TestLogger<GameWorkerService>();
        var regionService = Substitute.For<IMapRegionService>();
        var worker = CreateWorker(regionService, TimeSpan.FromDays(1), logger);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);
        worker.Dispose();

        Assert.IsFalse(logger.Entries.Any(entry => entry.Level == LogLevel.Error));
        regionService.DidNotReceive().FindAllRegions();
    }

    private static GameWorkerService CreateWorker(IMapRegion region, TimeSpan tickTimeSpan, TestLogger<GameWorkerService>? logger = null) =>
        CreateWorker(new[] { region }, tickTimeSpan, logger);

    private static GameWorkerService CreateWorker(
        IEnumerable<IMapRegion> regions,
        TimeSpan tickTimeSpan,
        TestLogger<GameWorkerService>? logger = null)
    {
        var regionService = Substitute.For<IMapRegionService>();
        regionService.FindAllRegions().Returns(regions);
        return CreateWorker(regionService, tickTimeSpan, logger);
    }

    private static GameWorkerService CreateWorker(
        IMapRegionService regionService,
        TimeSpan tickTimeSpan,
        TestLogger<GameWorkerService>? logger = null)
    {
        var scheduler = Substitute.For<IRsTaskService>();
        return new GameWorkerService(
            scheduler,
            regionService,
            Options.Create(new GameServerOptions
            {
                AuthenticationToken = string.Empty,
                ClientRevision = 0,
                ClientRevisionPatch = 0,
                TickTimeSpan = tickTimeSpan
            }),
            logger ?? new TestLogger<GameWorkerService>());
    }

    private static void ConfigureRegion(
        IMapRegion region,
        string name,
        ICollection<string> events,
        Func<Task> majorUpdate,
        Func<int> currentTick)
    {
        region.MajorUpdateTick().Returns(_ => majorUpdate());
        region.MajorClientPrepareUpdateTick().Returns(_ =>
        {
            events.Add($"prepare-{currentTick()}-{name}");
            return Task.CompletedTask;
        });
        region.MajorClientUpdateTick().Returns(_ =>
        {
            events.Add($"update-{currentTick()}-{name}");
            return Task.CompletedTask;
        });
        region.MajorClientUpdateResetTick().Returns(_ =>
        {
            events.Add($"reset-{currentTick()}-{name}");
            return Task.CompletedTask;
        });
    }

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
