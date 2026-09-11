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

}
