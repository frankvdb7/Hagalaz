using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MapRegionBackgroundServiceTests
{
    [TestMethod]
    public async Task ProcessRegionsOnceAsync_DoesNotDestroyRegionWhenExactIdleClaimFails()
    {
        var region = Substitute.For<IMapRegion>();
        const int regionId = 1;
        region.Id.Returns(regionId);
        region.State.Returns(MapRegionState.Ready);
        region.CanDestroy().Returns(true);
        var dimension = Substitute.For<IDimension>();
        dimension.Id.Returns(0);
        var regionService = Substitute.For<IMapRegionService>();
        regionService.FindAllDimensions().Returns(new[] { dimension });
        regionService.FindRegionsByDimension(0).Returns([]);
        regionService.FindIdleRegionsByDimension(0).Returns(new[] { region });
        regionService.TryRemoveIdleMapRegion(regionId, dimension.Id, region).Returns(false);

        var service = new MapRegionBackgroundService(
            regionService,
            Substitute.For<ILogger<MapRegionBackgroundService>>());

        await service.ProcessRegionsOnceAsync();

        await region.DidNotReceive().DestroyAsync();
    }

    [TestMethod]
    public async Task HostedWorker_DestroysExactDetachedRegionOutsideTheTick()
    {
        var region = Substitute.For<IMapRegion>();
        const int regionId = 1;
        region.Id.Returns(regionId);
        region.CanDestroy().Returns(true);
        var destructionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var destructionRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        region.DestroyAsync().Returns(_ =>
        {
            destructionStarted.TrySetResult();
            return destructionRelease.Task;
        });
        var dimension = Substitute.For<IDimension>();
        dimension.Id.Returns(1);
        var regionService = Substitute.For<IMapRegionService>();
        regionService.FindAllDimensions().Returns(new[] { dimension });
        regionService.FindRegionsByDimension(1).Returns([]);
        regionService.FindIdleRegionsByDimension(1).Returns(new[] { region });
        regionService.TryRemoveIdleMapRegion(regionId, dimension.Id, region).Returns(true);

        var service = new MapRegionBackgroundService(
            regionService,
            Substitute.For<ILogger<MapRegionBackgroundService>>());
        await service.StartAsync(CancellationToken.None);
        try
        {
            var tickHousekeeping = service.ProcessRegionsOnceAsync();
            await tickHousekeeping;

            Assert.IsTrue(tickHousekeeping.IsCompleted);
            regionService.Received(1).TryRemoveIdleMapRegion(regionId, dimension.Id, region);
            Assert.IsFalse(destructionRelease.Task.IsCompleted);

            await destructionStarted.Task;
            _ = region.Received(1).DestroyAsync();
        }
        finally
        {
            destructionRelease.TrySetResult();
            await service.StopAsync(CancellationToken.None);
        }
    }

}
