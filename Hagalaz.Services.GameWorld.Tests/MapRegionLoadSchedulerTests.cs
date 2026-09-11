using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
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
        public async Task EnsureLoadedAsync_SkipsReadyRegions()
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
            region.State.Returns(MapRegionState.Ready);

            await scheduler.StartAsync(CancellationToken.None);
            await scheduler.EnsureLoadedAsync(new[] { region });
            await scheduler.StopAsync(CancellationToken.None);

            await loader.DidNotReceive().LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task RequestLoad_DiscardedRegion_DoesNotInvokeLoader()
        {
            var loader = Substitute.For<IMapRegionLoader>();
            using var provider = new ServiceCollection()
                .AddScoped(_ => loader)
                .BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var region = CreateRegion(1, MapRegionState.Discarded);

            await scheduler.StartAsync(CancellationToken.None);
            scheduler.RequestLoad(region);
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
            var region = CreateRegion(1);

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
            var region = CreateRegion(1);

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
        public async Task RequestLoad_SkipsRegionAfterLoaderMarksItReady()
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
            var region = CreateRegion(1);

            await scheduler.StartAsync(CancellationToken.None);
            scheduler.RequestLoad(region);
            await loadCompleted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            region.State.Returns(MapRegionState.Ready);
            scheduler.RequestLoad(region);

            await scheduler.StopAsync(CancellationToken.None);

            await loader.Received(1).LoadAsync(region, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task EnsureLoadedAsync_WhenLoadFails_AllConcurrentWaitersObserveTheFailure()
        {
            var loadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseLoad = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var failure = new InvalidOperationException("test failure");
            var loader = Substitute.For<IMapRegionLoader>();
            loader.LoadAsync(Arg.Any<IMapRegion>(), Arg.Any<CancellationToken>())
                .Returns(async _ =>
                {
                    loadStarted.TrySetResult();
                    await releaseLoad.Task;
                    throw failure;
                });

            using var provider = new ServiceCollection()
                .AddScoped(_ => loader)
                .BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var region = CreateRegion(1);

            await scheduler.StartAsync(CancellationToken.None);
            var firstWaiter = scheduler.EnsureLoadedAsync(new[] { region });
            await loadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            var secondWaiter = scheduler.EnsureLoadedAsync(new[] { region });

            releaseLoad.TrySetResult();
            var firstFailure = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => firstWaiter);
            var secondFailure = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => secondWaiter);
            await scheduler.StopAsync(CancellationToken.None);

            Assert.AreSame(failure, firstFailure);
            Assert.AreSame(failure, secondFailure);
            await loader.Received(1).LoadAsync(region, Arg.Any<CancellationToken>());
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
            var region = CreateRegion(1);
            using var cancellation = new CancellationTokenSource();

            await scheduler.StartAsync(cancellation.Token);
            scheduler.RequestLoad(region);
            await loadStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

            cancellation.Cancel();
            await loadCanceled.Task.WaitAsync(TimeSpan.FromSeconds(1));
            await scheduler.StopAsync(CancellationToken.None);

            Assert.AreEqual(MapRegionState.Initializing, region.State);
            await loader.Received(1).LoadAsync(region, Arg.Any<CancellationToken>());
        }

        [TestMethod]
        public async Task RequestLoad_DuringShutdown_DoesNotThrow()
        {
            using var provider = new ServiceCollection().BuildServiceProvider();
            using var scheduler = new MapRegionLoadScheduler(
                provider.GetRequiredService<IServiceScopeFactory>(),
                Substitute.For<ILogger<MapRegionLoadScheduler>>());
            var region = CreateRegion(1);

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
            var region = CreateRegion(1);

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
            var region = CreateRegion(1);
            region.State.Returns(_ => releaseLoad.Task.IsCompleted ? MapRegionState.Ready : MapRegionState.Initializing);
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

        private static IMapRegion CreateRegion(int id, MapRegionState state = MapRegionState.Initializing)
        {
            var region = Substitute.For<IMapRegion>();
            region.Id.Returns(id);
            region.BaseLocation.Returns(Location.Create(id << 6, 0, 0, 0));
            region.State.Returns(state);
            return region;
        }

        private static IMapRegion CreateRegion(int id, ConcurrentDictionary<IMapRegion, bool> loaded)
        {
            var region = CreateRegion(id);
            region.State.Returns(_ => loaded.TryGetValue(region, out var isLoaded) && isLoaded
                ? MapRegionState.Ready
                : MapRegionState.Initializing);
            loaded[region] = false;
            return region;
        }

    }
}
