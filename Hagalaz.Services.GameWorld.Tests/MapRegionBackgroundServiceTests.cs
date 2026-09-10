using System.Collections.Generic;
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
        dimension.Regions.Returns(new Dictionary<int, IMapRegion>());
        var idleRegions = new Dictionary<int, IMapRegion> { [regionId] = region };
        dimension.IdleRegions.Returns(idleRegions);
        dimension.CanDestroy().Returns(false);
        var regionService = Substitute.For<IMapRegionService>();
        regionService.FindAllDimensions().Returns(new[] { dimension });
        regionService.TryTakeIdleMapRegionForDestroy(regionId, dimension.Id, region).Returns(false);

        var service = new MapRegionBackgroundService(
            regionService,
            Substitute.For<ILogger<MapRegionBackgroundService>>());

        await service.ProcessRegionsOnceAsync();

        await region.DidNotReceive().DestroyAsync();
    }

    [TestMethod]
    public async Task ProcessRegionsOnceAsync_RetriesPendingDestructionAndReleasesExactOwnershipAfterSuccess()
    {
        var region = Substitute.For<IMapRegion>();
        region.Id.Returns(1);
        region.BaseLocation.Returns(Location.Create(64, 64, 0, 3));
        region.DestructionState.Returns(MapRegionDestructionState.Destroyed);
        var dimension = Substitute.For<IDimension>();
        dimension.Id.Returns(3);
        dimension.Regions.Returns(new Dictionary<int, IMapRegion>());
        dimension.IdleRegions.Returns(new Dictionary<int, IMapRegion>());
        dimension.CanDestroy().Returns(false);
        var regionService = Substitute.For<IMapRegionService>();
        regionService.FindAllDimensions().Returns(new[] { dimension });
        regionService.FindPendingDestructionRegions(3).Returns(new[] { region });

        var service = new MapRegionBackgroundService(regionService, Substitute.For<ILogger<MapRegionBackgroundService>>());

        await service.ProcessRegionsOnceAsync();

        await region.Received(1).DestroyAsync();
        regionService.Received(1).TryCompleteMapRegionDestruction(region);
    }
}
