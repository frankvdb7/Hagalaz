using System;
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
    }
}
