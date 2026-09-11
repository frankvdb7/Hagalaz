using System.Collections.Concurrent;
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
    public void GetOrCreateMapRegion_RequestsInitialLoadForNewCanonicalRegion()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = CreateProvider(loadRequests);
        var service = CreateService(provider);

        var region = service.GetOrCreateMapRegion(1, 0, false);

        loadRequests.Received(1).RequestLoad(region);
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
    public void NonZeroDimensionRegion_PreservesDimensionAcrossResidencyOperations()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));
        Assert.AreEqual(1, dimension!.Id);

        var region = service.GetOrCreateMapRegion(1, dimension.Id, false);

        Assert.AreEqual(dimension.Id, region.BaseLocation.Dimension);
        Assert.IsTrue(service.TrySuspendMapRegion(region));
        Assert.AreSame(region, service.GetOrCreateMapRegion(region.Id, dimension.Id, true));
        Assert.IsTrue(service.IsCurrentMapRegion(region.Id, dimension.Id, region));
    }

    [TestMethod]
    public void CreateDynamicRegion_PreservesNonZeroDimensionForBothRegions()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));
        var source = Location.Create(64, 64, 0, dimension!.Id);
        var destination = Location.Create(128, 64, 0, dimension.Id);

        service.CreateDynamicRegion(source, destination);

        Assert.AreEqual(dimension.Id, service.GetMapRegion(source.RegionId, dimension.Id, false, false)!.BaseLocation.Dimension);
        var dynamicRegion = service.GetMapRegion(destination.RegionId, dimension.Id, false, false)!;
        Assert.AreEqual(dimension.Id, dynamicRegion.BaseLocation.Dimension);
        Assert.IsTrue(dynamicRegion.IsDynamic);
    }

    [TestMethod]
    public async Task TryRemoveEmptyDimension_DoesNotDetachDimensionThatPublishesARegion()
    {
        using var provider = CreateProvider();
        using var builder = new GatedLocationBuilder();
        var service = CreateService(provider, builder);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));

        var create = Task.Run(() => service.GetOrCreateMapRegion(1, dimension!.Id, false));
        builder.Started.Wait();
        builder.Release.Set();
        var region = await create;

        Assert.IsFalse(service.TryRemoveEmptyDimension(dimension));
        Assert.AreSame(region, service.GetMapRegion(region.Id, dimension.Id, false, false));
    }

    [TestMethod]
    public async Task TryRemoveEmptyDimension_WinsBeforeRegionPublicationWithoutOrphaningRegion()
    {
        using var provider = CreateProvider();
        using var builder = new GatedLocationBuilder();
        var service = CreateService(provider, builder);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));

        var create = Task.Run(() => service.GetOrCreateMapRegion(1, dimension!.Id, false));
        builder.Started.Wait();

        Assert.IsTrue(service.TryRemoveEmptyDimension(dimension));
        builder.Release.Set();
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => create);
        Assert.IsFalse(service.FindAllDimensions().Any(found => found.Id == dimension.Id));
    }

    [TestMethod]
    public void TryRemoveEmptyDimension_StaleDimensionCannotRemoveReplacement()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        Assert.IsTrue(service.TryCreateDimension(out var oldDimension));

        Assert.IsTrue(service.TryRemoveEmptyDimension(oldDimension!));
        Assert.IsTrue(service.TryCreateDimension(out var replacement));

        Assert.IsFalse(service.TryRemoveEmptyDimension(oldDimension));
        Assert.IsTrue(service.FindAllDimensions().Contains(replacement));
    }

    [TestMethod]
    public async Task GetOrCreateMapRegion_ConcurrentCreation_ReturnsOneCanonicalInstance()
    {
        const int callerCount = 16;
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = CreateProvider(loadRequests);
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
        loadRequests.Received(1).RequestLoad(regions[0]);
    }

    [TestMethod]
    public void GetOrCreateMapRegion_ReadyCanonicalRegionDoesNotRequestAnotherLoad()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = CreateProvider(loadRequests);
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0, false);
        region.MarkReady();

        Assert.AreSame(region, service.GetOrCreateMapRegion(1, 0, false));
        loadRequests.Received(1).RequestLoad(region);
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
    public void TryRemoveIdleMapRegion_FailsAfterRegionResumes()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0, false);
        region.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(region));

        var resumed = service.GetOrCreateMapRegion(1, 0, true);

        Assert.AreSame(region, resumed);
        Assert.IsFalse(service.TryRemoveIdleMapRegion(region.Id, 0, region));
        Assert.IsFalse(region.IsDestroyed);
    }

    [TestMethod]
    public void TryRemoveIdleMapRegion_WinsBeforeResumeAndForcesFreshCreation()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0, false);
        region.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(region));

        Assert.IsTrue(service.TryRemoveIdleMapRegion(region.Id, 0, region));
        var replacement = service.GetOrCreateMapRegion(region.Id, 0, true);

        Assert.AreNotSame(region, replacement);
        Assert.AreSame(replacement, service.GetMapRegion(region.Id, 0, false, false));
        Assert.IsFalse(service.FindAllDimensions().Single().IdleRegions.ContainsKey(region.Id));
    }

    [TestMethod]
    public async Task TryRemoveIdleMapRegion_ReleasesExactOwnerAfterRemoval()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));
        var region = service.GetOrCreateMapRegion(1, dimension!.Id, false);
        region.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(region));

        Assert.IsTrue(service.TryRemoveIdleMapRegion(region.Id, dimension.Id, region));
        Assert.IsTrue(dimension.CanDestroy());

        await region.DestroyAsync();
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
        Assert.IsFalse(service.TryRemoveIdleMapRegion(staleRegion.Id, 0, staleRegion));
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
    public void DimensionResidencyViews_AreStableSnapshots()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var dimension = service.FindAllDimensions().Single();
        var region = service.GetOrCreateMapRegion(1, dimension.Id, false);
        var snapshot = dimension.Regions;

        Assert.IsTrue(snapshot.ContainsKey(region.Id));
        Assert.IsTrue(service.TryRemoveMapRegion(region.Id, dimension.Id, region));
        Assert.IsTrue(snapshot.ContainsKey(region.Id));
        Assert.IsFalse(dimension.Regions.ContainsKey(region.Id));
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

    private static ServiceProvider CreateProvider(IMapRegionLoadScheduler? loadRequests = null) => new ServiceCollection()
        .AddSingleton(Substitute.For<INpcService>())
        .AddSingleton(loadRequests ?? Substitute.For<IMapRegionLoadScheduler>())
        .BuildServiceProvider();

    private static MapRegionService CreateService(IServiceProvider provider) => CreateService(provider, new LocationBuilder());

    private static MapRegionService CreateService(IServiceProvider provider, ILocationBuilder locationBuilder) => new(
        provider,
        locationBuilder,
        Substitute.For<IGameObjectBuilder>(),
        Substitute.For<IGroundItemBuilder>(),
        Substitute.For<ILogger<MapRegionService>>(),
        Substitute.For<IMapper>(),
        provider.GetRequiredService<IMapRegionLoadScheduler>());

    private sealed class BlockingLocationBuilder(Barrier creationGate) : ILocationBuilder
    {
        public ILocationX Create()
        {
            creationGate.SignalAndWait();
            return new LocationBuilder();
        }
    }

    private sealed class GatedLocationBuilder : ILocationBuilder, IDisposable
    {
        public ManualResetEventSlim Started { get; } = new();
        public ManualResetEventSlim Release { get; } = new();

        public ILocationX Create()
        {
            Started.Set();
            Release.Wait();
            return new LocationBuilder();
        }

        public void Dispose()
        {
            Started.Dispose();
            Release.Dispose();
        }
    }
}
