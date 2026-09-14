using System.Linq;
using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Collections.Generic;

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

        await service.ProcessRegionsOnceAsync(new Dictionary<int, ICharacter>());

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
            var tickHousekeeping = service.ProcessRegionsOnceAsync(new Dictionary<int, ICharacter>());
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

    [TestMethod]
    public async Task ProcessRegionsOnceAsync_DoesNotSuspendVisibleCanonicalRegion()
    {
        var region = Substitute.For<IMapRegion>();
        region.Id.Returns(1);
        region.State.Returns(MapRegionState.Ready);
        region.CanSuspend().Returns(true);
        var character = Substitute.For<ICharacter>();
        character.Viewport.VisibleRegions.Returns(new[] { region });
        var dimension = Substitute.For<IDimension>();
        dimension.Id.Returns(0);
        var regionService = Substitute.For<IMapRegionService>();
        regionService.FindAllDimensions().Returns(new[] { dimension });
        regionService.FindRegionsByDimension(0).Returns(new[] { region });
        regionService.FindIdleRegionsByDimension(0).Returns([]);

        var service = new MapRegionBackgroundService(
            regionService,
            Substitute.For<ILogger<MapRegionBackgroundService>>());

        await service.ProcessRegionsOnceAsync(new Dictionary<int, ICharacter> { [42] = character });

        regionService.DidNotReceive().TrySuspendMapRegion(region);
    }

    [TestMethod]
    public async Task ProcessRegionsOnceAsync_AfterVisibleRefresh_LeavesCanonicalRegionActiveForCollisionAndUpdates()
    {
        var loadScheduler = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = new ServiceCollection()
            .AddSingleton(Substitute.For<INpcService>())
            .BuildServiceProvider();
        var regionService = new MapRegionService(
            provider,
            new LocationBuilder(),
            Substitute.For<IGameObjectBuilder>(),
            Substitute.For<IGroundItemBuilder>(),
            Substitute.For<ILogger<MapRegionService>>(),
            Substitute.For<IMapper>(),
            loadScheduler);
        var location = Location.Create(64, 64, 0, 0);
        var region = regionService.GetOrCreateMapRegion(location.RegionId, location.Dimension);
        region.MarkReady();
        region.FlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, CollisionFlag.WallNorth);

        var character = Substitute.For<ICharacter>();
        character.Location.Returns(location);
        var mapSize = Substitute.For<IMapSize>();
        mapSize.Size.Returns(8);
        mapSize.Type.Returns(0);
        var viewport = new Viewport(character, regionService, mapSize);
        character.Viewport.Returns(viewport);

        viewport.RebuildView();
        Assert.AreSame(region, viewport.VisibleRegions.Single());
        Assert.IsTrue(regionService.TrySuspendMapRegion(region));

        viewport.RefreshVisibleRegions();

        Assert.AreSame(region, viewport.VisibleRegions.Single());
        Assert.AreSame(region, regionService.FindMapRegion(location.RegionId, location.Dimension));
        Assert.AreEqual(
            CollisionFlag.WallNorth,
            regionService.GetClippingFlag(location.X, location.Y, location.Z));

        var backgroundService = new MapRegionBackgroundService(
            regionService,
            Substitute.For<ILogger<MapRegionBackgroundService>>());
        await backgroundService.ProcessRegionsOnceAsync(new Dictionary<int, ICharacter> { [character.Index] = character });

        Assert.AreSame(region, regionService.FindMapRegion(location.RegionId, location.Dimension));
        Assert.IsEmpty(regionService.FindIdleRegionsByDimension(location.Dimension));
    }

}
