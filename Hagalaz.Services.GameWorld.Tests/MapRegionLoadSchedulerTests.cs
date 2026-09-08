using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class MapRegionLoadSchedulerTests
    {
        [TestMethod]
        public async Task EnsureLoadedAsync_WaitsForEveryRegionBeforeReturning()
        {
            var loadStarted = new ConcurrentDictionary<IMapRegion, TaskCompletionSource>();
            var releaseLoad = new ConcurrentDictionary<IMapRegion, TaskCompletionSource>();
            var loaded = new ConcurrentDictionary<IMapRegion, bool>();
            var loader = Substitute.For<IMapRegionLoader>();
            loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
                .Returns(async callInfo =>
                {
                    var region = callInfo.Arg<IMapRegion>()!;
                    loadStarted[region].TrySetResult();
                    await releaseLoad[region].Task;
                    loaded[region] = true;
                });

            using var provider = new ServiceCollection()
                .AddScoped(_ => loader)
                .BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var firstRegion = CreateRegion(1, loaded);
            var secondRegion = CreateRegion(2, loaded);
            loadStarted[firstRegion] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            loadStarted[secondRegion] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            releaseLoad[firstRegion] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            releaseLoad[secondRegion] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await scheduler.StartAsync(CancellationToken.None);
            var wait = scheduler.EnsureLoadedAsync(new[] { firstRegion, secondRegion });

            await loadStarted[firstRegion].Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.IsFalse(wait.IsCompleted);

            releaseLoad[firstRegion].TrySetResult();
            await loadStarted[secondRegion].Task.WaitAsync(TimeSpan.FromSeconds(1));
            Assert.IsFalse(wait.IsCompleted);

            releaseLoad[secondRegion].TrySetResult();
            await wait.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.IsTrue(loaded[firstRegion]);
            Assert.IsTrue(loaded[secondRegion]);
            await scheduler.StopAsync(CancellationToken.None);
            await loader.Received(1).LoadAsync(firstRegion, Arg.Any<CancellationToken>());
            await loader.Received(1).LoadAsync(secondRegion, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task EnsureLoadedAsync_SkipsAlreadyLoadedRegions()
        {
            var loader = Substitute.For<IMapRegionLoader>();
            using var provider = new ServiceCollection()
                .AddScoped(_ => loader)
                .BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(1);
            region.IsLoaded.Returns(true);

            await scheduler.StartAsync(CancellationToken.None);
            await scheduler.EnsureLoadedAsync(new[] { region });
            await scheduler.StopAsync(CancellationToken.None);

            await loader.DidNotReceive().LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task EnsureLoadedAsync_ReportsReadinessFailureWithoutRetrying()
        {
            var loader = Substitute.For<IMapRegionLoader>();
            loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);
            using var provider = new ServiceCollection()
                .AddScoped(_ => loader)
                .BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(1);
            region.IsLoaded.Returns(false);

            await scheduler.StartAsync(CancellationToken.None);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => scheduler.EnsureLoadedAsync(new[] { region }));
            await scheduler.StopAsync(CancellationToken.None);

            await loader.Received(1).LoadAsync(region, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task RequestLoad_IsNonBlockingAndDeduplicatesWhileLoadIsInFlight()
        {
            var loadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loadCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseLoad = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loader = Substitute.For<IMapRegionLoader>();
            loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
                .Returns(async _ =>
                {
                    loadStarted.TrySetResult();
                    await releaseLoad.Task;
                    loadCompleted.TrySetResult();
                });

            using var provider = new ServiceCollection()
                .AddScoped(_ => loader)
                .BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(1);
            region.IsLoaded.Returns(false);

            await scheduler.StartAsync(CancellationToken.None);

            scheduler.RequestLoad(region);
            await loadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            scheduler.RequestLoad(region);

            releaseLoad.TrySetResult();
            await loadCompleted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await scheduler.StopAsync(CancellationToken.None);

            await loader.Received(1).LoadAsync(region, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task RequestLoad_SkipsRegionAfterLoaderMarksItLoaded()
        {
            var loadCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loader = Substitute.For<IMapRegionLoader>();
            loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    loadCompleted.TrySetResult();
                    return Task.CompletedTask;
                });

            using var provider = new ServiceCollection()
                .AddScoped(_ => loader)
                .BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(1);
            region.IsLoaded.Returns(false);

            await scheduler.StartAsync(CancellationToken.None);
            scheduler.RequestLoad(region);
            await loadCompleted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            region.IsLoaded.Returns(true);
            scheduler.RequestLoad(region);

            await scheduler.StopAsync(CancellationToken.None);

            await loader.Received(1).LoadAsync(region, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task RequestLoad_RetriesRegionAfterLoaderFailure()
        {
            var firstAttemptFailed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondAttemptCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var attempts = 0;
            var loader = Substitute.For<IMapRegionLoader>();
            loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    if (Interlocked.Increment(ref attempts) == 1)
                    {
                        firstAttemptFailed.TrySetResult();
                        return Task.FromException(new InvalidOperationException("test failure"));
                    }

                    secondAttemptCompleted.TrySetResult();
                    return Task.CompletedTask;
                });

            using var provider = new ServiceCollection()
                .AddScoped(_ => loader)
                .BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(1);
            region.IsLoaded.Returns(false);

            await scheduler.StartAsync(CancellationToken.None);

            scheduler.RequestLoad(region);
            await firstAttemptFailed.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await scheduler.StopAsync(CancellationToken.None);

            using var retryScheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            await retryScheduler.StartAsync(CancellationToken.None);
            retryScheduler.RequestLoad(region);
            await secondAttemptCompleted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await retryScheduler.StopAsync(CancellationToken.None);

            Assert.AreEqual(2, attempts);
            await loader.Received(2).LoadAsync(region, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task RequestLoad_CanceledLoadDoesNotPublishReadiness()
        {
            var loadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loadCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loader = Substitute.For<IMapRegionLoader>();
            loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var cancellationToken = callInfo.Arg<CancellationToken>();
                    loadStarted.TrySetResult();
                    cancellationToken.Register(() => loadCanceled.TrySetResult());
                    return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                });

            using var provider = new ServiceCollection()
                .AddScoped(_ => loader)
                .BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(1);
            region.IsLoaded.Returns(false);
            using var cancellation = new CancellationTokenSource();

            await scheduler.StartAsync(cancellation.Token);
            scheduler.RequestLoad(region);
            await loadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            cancellation.Cancel();
            await loadCanceled.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await scheduler.StopAsync(CancellationToken.None);

            Assert.IsFalse(region.IsLoaded);
            await loader.Received(1).LoadAsync(region, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task RequestLoad_DuringShutdown_DoesNotThrow()
        {
            using var provider = new ServiceCollection().BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(1);
            region.IsLoaded.Returns(false);

            await scheduler.StartAsync(CancellationToken.None);
            await scheduler.StopAsync(CancellationToken.None);

            scheduler.RequestLoad(region);
        }

        [TestMethod]
        public async Task EnsureLoadedAsync_WhenSchedulerStops_CompletesWaitingCaller()
        {
            var loadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loader = Substitute.For<IMapRegionLoader>();
            loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    loadStarted.TrySetResult();
                    return Task.Delay(Timeout.InfiniteTimeSpan, callInfo.Arg<CancellationToken>());
                });

            using var provider = new ServiceCollection()
                .AddScoped(_ => loader)
                .BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(1);
            region.IsLoaded.Returns(false);

            await scheduler.StartAsync(CancellationToken.None);
            var wait = scheduler.EnsureLoadedAsync(new[] { region });
            await loadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            var stop = scheduler.StopAsync(CancellationToken.None);
            await Assert.ThrowsAsync<OperationCanceledException>(() => wait);
            await stop;
        }

        [TestMethod]
        public async Task EnsureLoadedAsync_CallerCancellationDoesNotCancelSharedLoad()
        {
            var loadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseLoad = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loadCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var loader = Substitute.For<IMapRegionLoader>();
            loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
                .Returns(async callInfo =>
                {
                    var cancellationToken = callInfo.Arg<CancellationToken>();
                    cancellationToken.Register(() => loadCanceled.TrySetResult());
                    loadStarted.TrySetResult();
                    await releaseLoad.Task;
                });

            using var provider = new ServiceCollection()
                .AddScoped(_ => loader)
                .BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(1);
            region.IsLoaded.Returns(_ => releaseLoad.Task.IsCompleted);
            using var callerCancellation = new CancellationTokenSource();

            await scheduler.StartAsync(CancellationToken.None);
            var wait = scheduler.EnsureLoadedAsync(new[] { region }, callerCancellation.Token);
            await loadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            callerCancellation.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => wait);
            Assert.IsFalse(loadCanceled.Task.IsCompleted);

            releaseLoad.TrySetResult();
            await scheduler.EnsureLoadedAsync(new[] { region });
            await scheduler.StopAsync(CancellationToken.None);

            await loader.Received(1).LoadAsync(region, Arg.Any<CancellationToken>());
        }

        private static IMapRegion CreateRegion(int id, ConcurrentDictionary<IMapRegion, bool> loaded)
        {
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(id);
            region.IsLoaded.Returns(_ => loaded.TryGetValue(region, out var isLoaded) && isLoaded);
            loaded[region] = false;
            return region;
        }
    }
}
