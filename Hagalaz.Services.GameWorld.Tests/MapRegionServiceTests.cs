using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MapRegionServiceTests
{
    [TestMethod]
    public void GetClippingFlag_ReturnsFloorBlockWhileRegionIsNotReady()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(67, 69, 0, 0);
        var region = service.GetOrCreateMapRegion(location.RegionId, location.Dimension, false);

        region.FlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, CollisionFlag.WallNorth);

        Assert.AreEqual(CollisionFlag.FloorBlock, service.GetClippingFlag(location.X, location.Y, location.Z));
    }

    [TestMethod]
    public void GetClippingFlag_ReturnsFloorBlockForDiscardedRegion()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(67, 69, 0, 0);
        var region = service.GetOrCreateMapRegion(location.RegionId, location.Dimension, false);
        region.MarkDiscarded();

        Assert.AreEqual(CollisionFlag.FloorBlock, service.GetClippingFlag(location.X, location.Y, location.Z));
    }

    [TestMethod]
    public void GetClippingFlag_ReturnsStoredCollisionAfterRegionIsReady()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(67, 69, 0, 0);
        var region = service.GetOrCreateMapRegion(location.RegionId, location.Dimension, false);
        region.FlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, CollisionFlag.WallNorth);

        region.MarkReady();

        Assert.AreEqual(CollisionFlag.WallNorth, service.GetClippingFlag(location.X, location.Y, location.Z));
    }

    [TestMethod]
    public void Pathfinder_RejectsStepIntoNotReadyRegion()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var pathFinder = new DumbPathFinder(service);

        Assert.IsFalse(pathFinder.CheckStep(Location.Create(67, 69, 0, 0), 1, 0, 1));
    }

    [TestMethod]
    public void TryRemoveMapRegion_RemovesOnlyTheExpectedActiveInstance()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(67, 69, 0, 0);
        var firstRegion = service.GetOrCreateMapRegion(location.RegionId, location.Dimension, false);

        Assert.IsTrue(service.TryRemoveMapRegion(firstRegion.Id, location.Dimension, firstRegion));

        var replacementRegion = service.GetOrCreateMapRegion(location.RegionId, location.Dimension, false);
        Assert.AreNotSame(firstRegion, replacementRegion);
        Assert.AreEqual(MapRegionState.Initializing, replacementRegion.State);
        Assert.IsFalse(service.TryRemoveMapRegion(firstRegion.Id, location.Dimension, firstRegion));
        Assert.AreSame(replacementRegion, service.GetMapRegion(location.RegionId, location.Dimension, false, false));
    }

    [TestMethod]
    public void FailedRegionRemoval_AllowsLaterRequestToCreateFreshRegion()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(67, 69, 0, 0);
        var failedRegion = service.GetOrCreateMapRegion(location.RegionId, location.Dimension, false);

        failedRegion.MarkDiscarded();
        Assert.IsTrue(service.TryRemoveMapRegion(failedRegion.Id, location.Dimension, failedRegion));

        var freshRegion = service.GetOrCreateMapRegion(location.RegionId, location.Dimension, false);

        Assert.AreNotSame(failedRegion, freshRegion);
        Assert.AreEqual(MapRegionState.Initializing, freshRegion.State);
    }

    [TestMethod]
    public async Task GetOrCreateMapRegion_ConcurrentCreation_ReturnsOneCanonicalInstance()
    {
        const int callerCount = 16;
        using var provider = CreateProvider();
        using var creationGate = new Barrier(callerCount);
        var service = CreateService(provider, new BlockingLocationBuilder(creationGate));
        var calls = Enumerable.Range(0, callerCount)
            .Select(_ => Task.Factory.StartNew(
                () => service.GetOrCreateMapRegion(1, 0, false),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default))
            .ToArray();

        var regions = await Task.WhenAll(calls);

        Assert.IsTrue(regions.All(region => ReferenceEquals(regions[0], region)));
        Assert.AreSame(regions[0], service.GetMapRegion(1, 0, false, false));
        Assert.AreEqual(1, service.FindRegionsByDimension(0).Count());
    }

    [TestMethod]
    public async Task GetOrCreateMapRegion_ConcurrentResume_ReturnsOneCanonicalActiveInstance()
    {
        const int callerCount = 16;
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0, false);
        region.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(region));
        using var startGate = new Barrier(callerCount);

        var calls = Enumerable.Range(0, callerCount)
            .Select(_ => Task.Run(() =>
            {
                startGate.SignalAndWait();
                return service.GetOrCreateMapRegion(1, 0, true);
            }))
            .ToArray();

        var regions = await Task.WhenAll(calls);

        Assert.IsTrue(regions.All(resumed => ReferenceEquals(region, resumed)));
        Assert.AreSame(region, service.GetMapRegion(1, 0, false, false));
        Assert.IsFalse(service.FindAllDimensions().Single().IdleRegions.ContainsKey(region.Id));
    }

    [TestMethod]
    public void TryTakeIdleMapRegionForDestroy_FailsAfterRegionResumes()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0, false);
        region.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(region));

        var resumed = service.GetOrCreateMapRegion(1, 0, true);

        Assert.AreSame(region, resumed);
        Assert.IsFalse(service.TryTakeIdleMapRegionForDestroy(region.Id, 0, region));
        Assert.IsFalse(region.IsDestroyed);
    }

    [TestMethod]
    public void TryTakeIdleMapRegionForDestroy_WinsBeforeResumeAndForcesFreshCreation()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0, false);
        region.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(region));

        Assert.IsTrue(service.TryTakeIdleMapRegionForDestroy(region.Id, 0, region));
        var replacement = service.GetOrCreateMapRegion(region.Id, 0, true);

        Assert.AreNotSame(region, replacement);
        Assert.AreSame(replacement, service.GetMapRegion(region.Id, 0, false, false));
        Assert.IsFalse(service.FindAllDimensions().Single().IdleRegions.ContainsKey(region.Id));
    }

    [TestMethod]
    public async Task TrySuspendMapRegion_ConcurrentResumeDoesNotCreateDuplicateResidency()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0, false);
        region.MarkReady();
        using var startGate = new Barrier(2);

        var suspend = Task.Run(() =>
        {
            startGate.SignalAndWait();
            return service.TrySuspendMapRegion(region);
        });
        var resume = Task.Run(() =>
        {
            startGate.SignalAndWait();
            return service.GetOrCreateMapRegion(region.Id, 0, true);
        });

        await Task.WhenAll(suspend, resume);
        var dimension = service.FindAllDimensions().Single();
        var residentRegions = dimension.Regions.Values.Concat(dimension.IdleRegions.Values).ToArray();

        Assert.AreEqual(1, residentRegions.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.AreSame(region, residentRegions.Single());
    }

    [TestMethod]
    public void StaleRegionOperations_CannotAffectReplacementActiveRegion()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var staleRegion = service.GetOrCreateMapRegion(1, 0, false);
        Assert.IsTrue(service.TryRemoveMapRegion(staleRegion.Id, 0, staleRegion));
        var currentRegion = service.GetOrCreateMapRegion(1, 0, false);

        Assert.IsFalse(service.TrySuspendMapRegion(staleRegion));
        Assert.IsFalse(service.TryTakeIdleMapRegionForDestroy(staleRegion.Id, 0, staleRegion));
        Assert.IsFalse(service.TryRemoveMapRegion(staleRegion.Id, 0, staleRegion));
        Assert.AreSame(currentRegion, service.GetMapRegion(1, 0, false, false));
        Assert.IsFalse(currentRegion.IsDestroyed);
    }

    [TestMethod]
    public void NewRegion_StartsInitializing()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);

        var region = service.GetOrCreateMapRegion(Location.Create(67, 69, 0, 0).RegionId, 0, false);

        Assert.AreEqual(MapRegionState.Initializing, region.State);
    }

    [TestMethod]
    public void RegionLifecycle_OnlyAllowsInitializingToTerminalState()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var readyRegion = service.GetOrCreateMapRegion(1, 0, false);
        var discardedRegion = service.GetOrCreateMapRegion(2, 0, false);

        readyRegion.MarkReady();
        discardedRegion.MarkDiscarded();

        Assert.AreEqual(MapRegionState.Ready, readyRegion.State);
        Assert.AreEqual(MapRegionState.Discarded, discardedRegion.State);
        Assert.ThrowsExactly<InvalidOperationException>(() => readyRegion.MarkDiscarded());
        Assert.ThrowsExactly<InvalidOperationException>(() => discardedRegion.MarkReady());
        discardedRegion.MarkDiscarded();
    }

    [TestMethod]
    public void IsCurrentMapRegion_RequiresTheExactCanonicalInstance()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(67, 69, 0, 0);
        var currentRegion = service.GetOrCreateMapRegion(location.RegionId, location.Dimension, false);
        var otherRegion = service.GetOrCreateMapRegion(2, 0, false);

        Assert.IsTrue(service.IsCurrentMapRegion(currentRegion.Id, location.Dimension, currentRegion));
        Assert.IsFalse(service.IsCurrentMapRegion(currentRegion.Id, location.Dimension, otherRegion));
    }

    private static ServiceProvider CreateProvider() => new ServiceCollection()
        .AddSingleton(Substitute.For<INpcService>())
        .BuildServiceProvider();

    private static MapRegionService CreateService(IServiceProvider provider) => CreateService(provider, new LocationBuilder());

    private static MapRegionService CreateService(IServiceProvider provider, ILocationBuilder locationBuilder) => new(
        provider,
        locationBuilder,
        Substitute.For<IGameObjectBuilder>(),
        Substitute.For<IGroundItemBuilder>(),
        Substitute.For<ILogger<MapRegionService>>(),
        Substitute.For<IMapper>());

    private sealed class BlockingLocationBuilder(Barrier creationGate) : ILocationBuilder
    {
        public ILocationX Create()
        {
            creationGate.SignalAndWait();
            return new LocationBuilder();
        }
    }
}
