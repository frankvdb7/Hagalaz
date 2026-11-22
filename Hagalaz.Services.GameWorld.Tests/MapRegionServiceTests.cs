using System.Collections.Concurrent;
using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Hagalaz.Services.GameWorld.Model.Maps.GameObjects;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MapRegionServiceTests
{
    [TestMethod]
    [DataRow(72)]
    [DataRow(104)]
    [DataRow(168)]
    public void GetMapRegionsWithinRange_CoversEveryRegionOfTheClientChunkGridAtRegionEdges(int mapSizeTiles)
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var mapSize = Substitute.For<IMapSize>();
        mapSize.Size.Returns(mapSizeTiles);

        // Parts 0 and 7 are the first and last part of a region, where the grid edge crosses a region border.
        foreach (var (partX, partY) in new[] { (400, 400), (407, 407), (400, 407), (407, 400), (403, 404) })
        {
            var location = Location.Create(partX * 8, partY * 8, 0, 0);
            var radius = mapSizeTiles >> 4;
            var expected = new List<int>();
            for (var regionX = (partX - radius) / 8; regionX <= (partX + radius) / 8; regionX++)
            {
                for (var regionY = (partY - radius) / 8; regionY <= (partY + radius) / 8; regionY++)
                {
                    expected.Add(regionY + (regionX << 8));
                }
            }

            var actual = service.GetMapRegionsWithinRange(location, mapSize).Select(region => region.Id).ToList();

            CollectionAssert.AreEqual(expected, actual, $"size {mapSizeTiles}, part ({partX}, {partY})");
            // every part of the client's grid lies in one of the returned regions
            for (var gridX = partX - radius; gridX < partX - radius + (mapSizeTiles >> 3); gridX++)
            {
                for (var gridY = partY - radius; gridY < partY - radius + (mapSizeTiles >> 3); gridY++)
                {
                    CollectionAssert.Contains(actual, (gridX / 8 << 8) | (gridY / 8));
                }
            }
        }
    }

    [TestMethod]
    public void GetClippingFlag_ReturnsFloorBlockWhileRegionIsNotReady()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(67, 69, 0, 0);
        var region = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);

        region.FlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, CollisionFlag.WallNorth);

        Assert.AreEqual(CollisionFlag.FloorBlock | CollisionFlag.ObjectBlock, service.GetClippingFlag(location.X, location.Y, location.Z));
    }

    [TestMethod]
    public void GetOrCreateMapRegion_RequestsInitialLoadForNewCanonicalRegion()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = CreateProvider(loadRequests);
        var service = CreateService(provider);

        var region = service.GetOrCreateMapRegion(1, 0);

        loadRequests.Received(1).RequestLoad(region);
    }

    [TestMethod]
    public void GetClippingFlag_ReturnsFloorBlockForDiscardedRegion()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(67, 69, 0, 0);
        var region = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);
        region.MarkDiscarded();

        Assert.AreEqual(CollisionFlag.FloorBlock | CollisionFlag.ObjectBlock, service.GetClippingFlag(location.X, location.Y, location.Z));
    }

    [TestMethod]
    public void GetClippingFlag_ReturnsStoredCollisionAfterRegionIsReady()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(67, 69, 0, 0);
        var region = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);
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
        var firstRegion = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);

        Assert.IsTrue(service.TryRemoveMapRegion(firstRegion.Id, location.Dimension, firstRegion));

        var replacementRegion = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);
        Assert.AreNotSame(firstRegion, replacementRegion);
        Assert.AreEqual(MapRegionState.Initializing, replacementRegion.State);
        Assert.IsFalse(service.TryRemoveMapRegion(firstRegion.Id, location.Dimension, firstRegion));
        Assert.AreSame(replacementRegion, service.FindMapRegion(location.RegionId, location.Dimension));
    }

    [TestMethod]
    public void FailedRegionRemoval_AllowsLaterRequestToCreateFreshRegion()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(67, 69, 0, 0);
        var failedRegion = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);

        failedRegion.MarkDiscarded();
        Assert.IsTrue(service.TryRemoveMapRegion(failedRegion.Id, location.Dimension, failedRegion));

        var freshRegion = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);

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

        var region = service.GetOrCreateMapRegion(1, dimension.Id);

        Assert.AreEqual(dimension.Id, region.BaseLocation.Dimension);
        Assert.IsTrue(service.TrySuspendMapRegion(region));
        Assert.AreSame(region, service.GetOrCreateMapRegion(region.Id, dimension.Id));
        Assert.IsTrue(service.IsCurrentMapRegion(region.Id, dimension.Id, region));
    }

    [TestMethod]
    public void CreateDynamicRegion_PreservesNonZeroDimensionForBothRegions()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        loadRequests.EnsureLoadedAsync(Arg.Any<IEnumerable<IMapRegion>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        using var provider = CreateProvider(loadRequests);
        var service = CreateService(provider);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));
        var source = Location.Create(64, 64, 0, dimension!.Id);
        var destination = Location.Create(128, 64, 0, dimension.Id);
        service.GetOrCreateMapRegion(source.RegionId, source.Dimension).MarkReady();

        service.CreateDynamicRegion(source, destination);
        provider.GetRequiredService<IRsTaskService>().Tick();

        Assert.AreEqual(dimension.Id, service.FindMapRegion(source.RegionId, dimension.Id)!.BaseLocation.Dimension);
        var dynamicRegion = service.GetOrCreateMapRegion(destination.RegionId, dimension.Id)!;
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

        var create = Task.Run(() => service.GetOrCreateMapRegion(1, dimension!.Id));
        builder.Started.Wait();
        builder.Release.Set();
        var region = await create;

        Assert.IsFalse(service.TryRemoveEmptyDimension(dimension));
        Assert.AreSame(region, service.FindMapRegion(region.Id, dimension.Id));
    }

    [TestMethod]
    public async Task TryRemoveEmptyDimension_WinsBeforeRegionPublicationWithoutOrphaningRegion()
    {
        using var provider = CreateProvider();
        using var builder = new GatedLocationBuilder();
        var service = CreateService(provider, builder);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));

        var create = Task.Run(() => service.GetOrCreateMapRegion(1, dimension!.Id));
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
                () => service.GetOrCreateMapRegion(1, 0),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default))
            .ToArray();

        var regions = await Task.WhenAll(calls);

        Assert.IsTrue(regions.All(region => ReferenceEquals(regions[0], region)));
        Assert.AreSame(regions[0], service.FindMapRegion(1, 0));
        Assert.AreEqual(1, service.FindRegionsByDimension(0).Count());
        loadRequests.Received(1).RequestLoad(regions[0]);
    }

    [TestMethod]
    public void GetOrCreateMapRegion_ReadyCanonicalRegionDoesNotRequestAnotherLoad()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = CreateProvider(loadRequests);
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0);
        region.MarkReady();

        Assert.AreSame(region, service.FindMapRegion(1, 0));
        loadRequests.Received(1).RequestLoad(region);
    }

    [TestMethod]
    public void FindMapRegion_DoesNotResumeIdleRegion()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0);
        region.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(region));

        Assert.AreSame(region, service.FindMapRegion(1, 0));
        Assert.IsTrue(service.FindIdleRegionsByDimension(0).Contains(region));
    }

    [TestMethod]
    public async Task GetOrCreateMapRegion_ConcurrentResume_ReturnsOneCanonicalActiveInstance()
    {
        const int callerCount = 16;
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0);
        region.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(region));
        using var startGate = new Barrier(callerCount);

        var calls = Enumerable.Range(0, callerCount)
            .Select(_ => Task.Run(() =>
            {
                startGate.SignalAndWait();
                return service.GetOrCreateMapRegion(1, 0);
            }))
            .ToArray();

        var regions = await Task.WhenAll(calls);

        Assert.IsTrue(regions.All(resumed => ReferenceEquals(region, resumed)));
        Assert.AreSame(region, service.FindMapRegion(1, 0));
        Assert.IsFalse(service.FindIdleRegionsByDimension(0).Contains(region));
    }

    [TestMethod]
    public async Task TryCreateDimension_ConcurrentCallersAllocateUniqueDimensions()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        const int callerCount = 32;

        var dimensions = await Task.WhenAll(Enumerable.Range(0, callerCount)
            .Select(_ => Task.Run(() => service.TryCreateDimension(out var dimension) ? dimension : null)));

        Assert.IsTrue(dimensions.All(dimension => dimension is not null));
        Assert.AreEqual(callerCount, dimensions.Select(dimension => dimension!.Id).Distinct().Count());
        Assert.AreEqual(callerCount + 1, service.FindAllDimensions().Count);
    }

    [TestMethod]
    public void MutationThroughService_ResumesIdleRegionBeforeApplyingChange()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0);
        Assert.IsTrue(service.TrySuspendMapRegion(region));

        service.FlagCollision(Location.Create(1, 65, 0, 0), CollisionFlag.WallNorth);

        Assert.AreSame(region, service.FindMapRegion(region.Id, 0));
        Assert.IsFalse(service.FindIdleRegionsByDimension(0).Contains(region));
        Assert.AreEqual(CollisionFlag.WallNorth, region.GetCollision(1, 1, 0));
    }

    [TestMethod]
    public void CollisionMutation_DoesNotMutateStaleRegionInstance()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var staleRegion = service.GetOrCreateMapRegion(1, 0);
        Assert.IsTrue(service.TryRemoveMapRegion(staleRegion.Id, 0, staleRegion));

        var currentRegion = service.GetOrCreateMapRegion(1, 0);
        var location = Location.Create(1, 65, 0, 0);

        service.FlagCollision(location, CollisionFlag.WallNorth);

        Assert.AreSame(currentRegion, service.FindMapRegion(1, 0));
        Assert.AreEqual(CollisionFlag.WallNorth, currentRegion.GetCollision(1, 1, 0));
        Assert.AreEqual(CollisionFlag.Walkable, staleRegion.GetCollision(1, 1, 0));
    }

    [TestMethod]
    public async Task AttachCharacter_SerializesMembershipWithSuspension()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(64, 64, 0, 0);
        var region = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);
        var addStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseAdd = new ManualResetEventSlim();
        var character = Substitute.For<ICharacter>();
        character.Location.Returns(location);
        character.CanSuspend().Returns(false);
        character.Index.Returns(_ =>
        {
            addStarted.TrySetResult();
            releaseAdd.Wait();
            return 1;
        });

        var attachTask = Task.Factory.StartNew(
            () => service.AttachCharacter(character),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        await addStarted.Task;
        var suspendTask = Task.Factory.StartNew(
            () => service.TrySuspendMapRegion(region),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        try
        {
            Assert.IsFalse(suspendTask.IsCompleted);
        }
        finally
        {
            releaseAdd.Set();
        }

        var attachedRegion = await attachTask;
        Assert.IsFalse(await suspendTask);
        Assert.AreSame(region, attachedRegion);
        Assert.AreSame(region, service.FindMapRegion(region.Id, location.Dimension));
        Assert.IsTrue(region.FindAllCharacters().Contains(character));
    }

    [TestMethod]
    public async Task AttachNpc_SerializesMembershipWithSuspension()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(128, 64, 0, 0);
        var region = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);
        var addStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var releaseAdd = new ManualResetEventSlim();
        var npc = Substitute.For<INpc>();
        npc.Location.Returns(location);
        npc.CanSuspend().Returns(false);
        npc.Index.Returns(_ =>
        {
            addStarted.TrySetResult();
            releaseAdd.Wait();
            return 1;
        });

        var attachTask = Task.Factory.StartNew(
            () => service.AttachNpc(npc),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        await addStarted.Task;
        var suspendTask = Task.Factory.StartNew(
            () => service.TrySuspendMapRegion(region),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        try
        {
            Assert.IsFalse(suspendTask.IsCompleted);
        }
        finally
        {
            releaseAdd.Set();
        }

        var attachedRegion = await attachTask;
        Assert.IsFalse(await suspendTask);
        Assert.AreSame(region, attachedRegion);
        Assert.AreSame(region, service.FindMapRegion(region.Id, location.Dimension));
        Assert.IsTrue(region.FindAllNpcs().Contains(npc));
    }

    [TestMethod]
    public void TrySuspendMapRegion_EvaluatesScriptEligibilityOutsideResidencyGate()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(64, 64, 0, 0);
        var region = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);
        region.MarkReady();

        var script = Substitute.For<IGameObjectScript>();
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.SizeX.Returns(1);
        definition.SizeY.Returns(1);
        definition.ClipType.Returns(0);
        definition.Gateway.Returns(false);
        definition.Solid.Returns(false);
        var gameObject = new GameObject(
            1,
            location,
            0,
            ShapeType.GroundDefault,
            false,
            definition,
            script);
        region.Add(gameObject);

        var unrelatedRegionCreated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        script.CanSuspend().Returns(_ =>
        {
            var unrelatedRegion = Task.Factory.StartNew(
                () => service.GetOrCreateMapRegion(2, 0),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            unrelatedRegion.GetAwaiter().GetResult();
            unrelatedRegionCreated.SetResult();
            return true;
        });

        Assert.IsTrue(service.TrySuspendMapRegion(region));
        Assert.IsTrue(unrelatedRegionCreated.Task.IsCompletedSuccessfully);
    }

    [TestMethod]
    public void TrySuspendMapRegion_RejectsNonSuspendableNpcAttachedAfterEligibilityWasObserved()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var location = Location.Create(64, 64, 0, 0);
        var region = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);
        region.MarkReady();

        var existingNpc = Substitute.For<INpc>();
        existingNpc.Location.Returns(location);
        existingNpc.Index.Returns(1);
        existingNpc.CanSuspend().Returns(true);
        service.AttachNpc(existingNpc);

        var beginAttach = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attachCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var nonSuspendableNpc = Substitute.For<INpc>();
        nonSuspendableNpc.Location.Returns(location);
        nonSuspendableNpc.Index.Returns(2);
        nonSuspendableNpc.CanSuspend().Returns(false);
        var attachTask = Task.Factory.StartNew(
            () =>
            {
                beginAttach.Task.GetAwaiter().GetResult();
                service.AttachNpc(nonSuspendableNpc);
                attachCompleted.SetResult();
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        existingNpc.CanSuspend().Returns(_ =>
        {
            beginAttach.SetResult();
            attachCompleted.Task.GetAwaiter().GetResult();
            return true;
        });

        Assert.IsFalse(service.TrySuspendMapRegion(region));
        attachTask.GetAwaiter().GetResult();
        Assert.AreSame(region, service.FindMapRegion(region.Id, location.Dimension));
        Assert.IsTrue(region.FindAllNpcs().Contains(nonSuspendableNpc));
    }

    [TestMethod]
    public void CreateDynamicRegion_RejectsIdleDestinationWithoutResumingOrMutating()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        loadRequests.EnsureLoadedAsync(Arg.Any<IEnumerable<IMapRegion>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        using var provider = CreateProvider(loadRequests);
        var service = CreateService(provider);
        var source = Location.Create(64, 64, 0, 0);
        var destination = Location.Create(128, 64, 0, 0);
        service.GetOrCreateMapRegion(source.RegionId, source.Dimension).MarkReady();
        var destinationRegion = service.GetOrCreateMapRegion(destination.RegionId, destination.Dimension);
        destinationRegion.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(destinationRegion));

        service.CreateDynamicRegion(source, destination);
        provider.GetRequiredService<IRsTaskService>().Tick();

        Assert.AreSame(destinationRegion, service.FindMapRegion(destination.RegionId, destination.Dimension));
        Assert.IsTrue(service.FindIdleRegionsByDimension(destination.Dimension).Contains(destinationRegion));
        Assert.IsFalse(destinationRegion.IsDynamic);
        Assert.AreEqual(MapRegionState.Ready, destinationRegion.State);
    }

    [TestMethod]
    public void CreateDynamicRegion_DoesNotCreateOrLoadAStandardSourceAndNeverLoadsTheDestination()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        var templateLoader = CreateTemplateLoader();
        using var provider = CreateProvider(loadRequests, templateLoader);
        var service = CreateService(provider);
        var source = Location.Create(64, 64, 0, 0);
        var destination = Location.Create(128, 64, 0, 0);

        service.CreateDynamicRegion(source, destination);
        provider.GetRequiredService<IRsTaskService>().Tick();

        var destinationRegion = service.FindMapRegion(destination.RegionId, destination.Dimension);
        Assert.IsNotNull(destinationRegion);
        Assert.AreEqual(MapRegionState.Ready, destinationRegion.State);
        Assert.IsTrue(destinationRegion.IsDynamic);
        Assert.IsNull(service.FindMapRegion(source.RegionId, source.Dimension));
        loadRequests.DidNotReceiveWithAnyArgs().RequestLoad(default!);
        loadRequests.DidNotReceiveWithAnyArgs().EnsureLoadedAsync(default!, default);
        templateLoader.Received(1).LoadAsync(source.RegionId, Arg.Any<int[]>(), Arg.Any<CancellationToken>());
        templateLoader.ReceivedWithAnyArgs(1).LoadAsync(default, default!, default);
    }

    [TestMethod]
    public async Task CreateDynamicRegion_ExposesNothingUntilPopulatedAndThenPublishesAReadyRegion()
    {
        using var pending = StartPendingCreation(_ => (StandardSource, Location.Create(640, 640, 0, 0)));

        Assert.IsNull(pending.Service.FindMapRegion(pending.Destination.RegionId, pending.Destination.Dimension), "The destination is invisible while templates load.");

        pending.Gate.SetResult();
        var published = await WaitForRegionAsync(pending.Service, pending.Tasks, pending.Destination);

        Assert.AreEqual(MapRegionState.Ready, published.State);
        Assert.IsTrue(published.IsDynamic);
        var part = published.GetRegionPartByLocalPart(3, 4, 2);
        Assert.AreEqual(50 * 8 + 3, part.DrawRegionPartX);
        Assert.AreEqual(51 * 8 + 4, part.DrawRegionPartY);
    }

    [TestMethod]
    public async Task CreateDynamicRegion_FromAPendingDynamicSourceFailsInsteadOfCopyingStandardCoordinates()
    {
        using var pending = StartPendingCreation(_ => (StandardSource, Location.Create(640, 640, 0, 0)));
        var instanceB = Location.Create(1280, 640, 0, 0);

        pending.Service.CreateDynamicRegion(pending.Destination, instanceB);
        pending.Tasks.Tick();
        pending.Gate.SetResult();
        var a = await WaitForRegionAsync(pending.Service, pending.Tasks, pending.Destination);
        pending.Tasks.Tick();

        Assert.AreEqual(MapRegionState.Ready, a.State);
        Assert.IsNull(pending.Service.FindMapRegion(instanceB.RegionId, instanceB.Dimension), "B must not be built from A's cache coordinates.");
        await pending.Loader.DidNotReceive().LoadAsync(pending.Destination.RegionId, Arg.Any<int[]>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task CreateDynamicRegion_WhenAnOrdinaryRegionAppearsMeanwhile_FailsAndLeavesItUntouched()
    {
        using var pending = StartPendingCreation(_ => (StandardSource, Location.Create(640, 640, 0, 0)));

        var ordinary = pending.Service.GetOrCreateMapRegion(pending.Destination.RegionId, pending.Destination.Dimension);
        pending.Gate.SetResult();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            pending.Tasks.Tick();
            await Task.Delay(10);
        }

        Assert.AreSame(ordinary, pending.Service.FindMapRegion(pending.Destination.RegionId, pending.Destination.Dimension));
        Assert.IsFalse(ordinary.IsDynamic);
    }

    [TestMethod]
    public void GetMapRegionsWithinRange_WhileADynamicDestinationIsPending_DoesNotFailAViewportRebuild()
    {
        var neighbour = Location.Create(640 - 64, 640, 0, 0);
        using var pending = StartPendingCreation(service =>
        {
            service.GetOrCreateMapRegion(neighbour.RegionId, neighbour.Dimension).MarkReady();
            return (StandardSource, Location.Create(640, 640, 0, 0));
        });
        var character = Substitute.For<ICharacter>();
        character.Location.Returns(neighbour);
        var mapSize = Substitute.For<IMapSize>();
        mapSize.Size.Returns(104);
        var viewport = new Viewport(character, pending.Service, mapSize);

        viewport.RebuildView();

        Assert.IsTrue(viewport.VisibleRegions.Any(region => region.Id == neighbour.RegionId));
    }

    [TestMethod]
    public void TryRemoveEmptyDimension_WhileADynamicRegionIsPending_KeepsTheDimension()
    {
        IDimension? dimension = null;
        using var pending = StartPendingCreation(service =>
        {
            Assert.IsTrue(service.TryCreateDimension(out dimension));
            return (StandardSource, Location.Create(640, 640, 0, dimension!.Id));
        });

        Assert.IsFalse(pending.Service.TryRemoveEmptyDimension(dimension!));
    }

    [TestMethod]
    public async Task CreateDynamicRegion_PopulationDoesNotHoldTheResidencyGateAndSignInWaitsForIt()
    {
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var script = Substitute.For<IGameObjectScript>();
        script.When(candidate => candidate.OnSpawn()).Do(_ =>
        {
            entered.Set();
            release.Wait(TimeSpan.FromSeconds(5));
        });
        var templateLoader = CreateTemplateLoader(regionId => new MapRegionTemplate(regionId,
            [new MapRegionTemplateObject(42, 3, 4, 0, 0, ShapeType.GroundDefault, CreateSolidDefinition(), true)], []));
        using var loadScheduler = new MapRegionLoadScheduler(Substitute.For<IServiceScopeFactory>(), Substitute.For<ILogger<MapRegionLoadScheduler>>());
        using var provider = CreateProvider(loadScheduler, templateLoader);
        var service = CreateService(provider, new LocationBuilder(), CreateGameObjectBuilder(script));
        var destination = Location.Create(640, 640, 0, 0);
        var unrelated = Location.Create(64, 64, 0, 0);
        var taskService = provider.GetRequiredService<IRsTaskService>();
        service.CreateDynamicRegion(Location.Create(3200, 3264, 0, 0), destination);

        var population = Task.Run(() => taskService.Tick());
        Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)), "Population never reached the object spawn.");

        // while an object script runs, other threads are not blocked, and the region is not usable yet
        var observed = Task.Run(() => (
            Destination: service.FindMapRegion(destination.RegionId, destination.Dimension),
            Unrelated: service.GetOrCreateMapRegion(unrelated.RegionId, unrelated.Dimension)));
        Assert.IsTrue(observed.Wait(TimeSpan.FromSeconds(2)), "A lookup waited on the dynamic region's population.");
        Assert.AreEqual(MapRegionState.Initializing, observed.Result.Destination?.State);
        Assert.IsNotNull(observed.Result.Unrelated);

        // sign-in waits for the population instead of failing on the initializing dynamic region
        var signIn = loadScheduler.EnsureLoadedAsync([observed.Result.Destination!]);
        await Task.Delay(50);
        Assert.IsFalse(signIn.IsCompleted, "Sign-in must wait for the dynamic population.");

        release.Set();
        await population;
        await signIn.WaitAsync(TimeSpan.FromSeconds(2));
        var published = service.FindMapRegion(destination.RegionId, destination.Dimension);
        Assert.AreEqual(MapRegionState.Ready, published!.State);
        Assert.IsTrue(published.IsDynamic);
    }

    [TestMethod]
    public void CreateDynamicRegion_ResumesAnIdleDynamicSource()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = CreateProvider(loadRequests);
        var service = CreateService(provider);
        var standard = Location.Create(3200, 3264, 0, 0);
        var firstDynamic = Location.Create(640, 640, 0, 0);
        var secondDynamic = Location.Create(1280, 640, 0, 0);
        var taskService = provider.GetRequiredService<IRsTaskService>();
        service.CreateDynamicRegion(standard, firstDynamic);
        taskService.Tick();
        var first = service.FindMapRegion(firstDynamic.RegionId, firstDynamic.Dimension)!;
        Assert.AreEqual(MapRegionState.Ready, first.State);
        Assert.IsTrue(service.TrySuspendMapRegion(first));
        Assert.IsTrue(service.FindIdleRegionsByDimension(0).Contains(first));

        service.CreateDynamicRegion(firstDynamic, secondDynamic);
        taskService.Tick();

        var second = service.FindMapRegion(secondDynamic.RegionId, secondDynamic.Dimension)!;
        Assert.AreEqual(MapRegionState.Ready, second.State);
        Assert.IsFalse(service.FindIdleRegionsByDimension(0).Contains(first), "The source was resumed.");
        Assert.AreEqual(50 * 8 + 3, second.GetRegionPartByLocalPart(3, 4, 2).DrawRegionPartX);
        loadRequests.DidNotReceiveWithAnyArgs().RequestLoad(default!);
        loadRequests.DidNotReceiveWithAnyArgs().EnsureLoadedAsync(default!, default);
    }

    [TestMethod]
    public void CreateDynamicRegion_CancellationNeverExposesTheDestinationAndReleasesItsReservation()
    {
        using var provider = CreateProvider(templateLoader: CreateGatedTemplateLoader(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)));
        var service = CreateService(provider);
        var source = Location.Create(64, 64, 0, 0);
        var destination = Location.Create(128, 64, 0, 0);

        var handle = service.CreateDynamicRegion(source, destination);
        var taskService = provider.GetRequiredService<IRsTaskService>();
        taskService.Tick();
        Assert.IsNull(service.FindMapRegion(destination.RegionId, destination.Dimension));

        handle.Cancel();
        taskService.Tick();
        taskService.Tick();

        Assert.IsNull(service.FindMapRegion(destination.RegionId, destination.Dimension));
        var standard = service.GetOrCreateMapRegion(destination.RegionId, destination.Dimension);
        Assert.IsFalse(standard.IsDynamic, "The reservation is released, so the id can hold an ordinary region again.");
    }

    [TestMethod]
    public void CreateDynamicRegion_RejectsExistingActiveDestinationWithoutMutation()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = CreateProvider(loadRequests);
        var service = CreateService(provider);
        var source = Location.Create(64, 64, 0, 0);
        var destination = Location.Create(128, 64, 0, 0);
        var sourceRegion = service.GetOrCreateMapRegion(source.RegionId, source.Dimension);
        var destinationRegion = service.GetOrCreateMapRegion(destination.RegionId, destination.Dimension);
        sourceRegion.MarkReady();
        destinationRegion.MarkReady();
        destinationRegion.FlagCollision(0, 0, 0, CollisionFlag.WallNorth);

        service.CreateDynamicRegion(source, destination);
        provider.GetRequiredService<IRsTaskService>().Tick();

        Assert.AreSame(destinationRegion, service.FindMapRegion(destination.RegionId, destination.Dimension));
        Assert.AreEqual(MapRegionState.Ready, destinationRegion.State);
        Assert.IsFalse(destinationRegion.IsDynamic);
        Assert.AreEqual(CollisionFlag.WallNorth, service.GetClippingFlag(destination.X, destination.Y, destination.Z));
        loadRequests.DidNotReceiveWithAnyArgs().EnsureLoadedAsync(default!, default);
    }

    [TestMethod]
    public void CreateDynamicRegion_RejectsIdleDestinationWithoutResumingOrMutatingEvenWhenSourceIsCanceled()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = CreateProvider(loadRequests);
        var service = CreateService(provider);
        var source = Location.Create(64, 64, 0, 0);
        var destination = Location.Create(128, 64, 0, 0);
        var destinationRegion = service.GetOrCreateMapRegion(destination.RegionId, destination.Dimension);
        destinationRegion.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(destinationRegion));

        var handle = service.CreateDynamicRegion(source, destination);
        var taskService = provider.GetRequiredService<IRsTaskService>();
        taskService.Tick();
        handle.Cancel();
        taskService.Tick();

        Assert.AreSame(destinationRegion, service.FindMapRegion(destination.RegionId, destination.Dimension));
        Assert.IsTrue(service.FindIdleRegionsByDimension(destination.Dimension).Contains(destinationRegion));
        Assert.AreEqual(MapRegionState.Ready, destinationRegion.State);
        Assert.IsFalse(destinationRegion.IsDynamic);
    }

    [TestMethod]
    public void CreateDynamicRegion_WhenCanceledAfterPopulationCommit_DoesNotDiscardCreatedRegion()
    {
        using var cancellation = new CancellationTokenSource();
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        loadRequests.EnsureLoadedAsync(Arg.Any<IEnumerable<IMapRegion>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var gameObjectBuilder = Substitute.For<IGameObjectBuilder>();
        var objectIdBuilder = Substitute.For<IGameObjectId>();
        var objectLocationBuilder = Substitute.For<IGameObjectLocation>();
        var objectOptionalBuilder = Substitute.For<IGameObjectOptional>();
        GameObject? copiedObject = null;
        ILocation? copiedLocation = null;
        gameObjectBuilder.Create().Returns(objectIdBuilder);
        objectIdBuilder.WithId(Arg.Any<int>()).Returns(objectLocationBuilder);
        objectLocationBuilder.WithLocation(Arg.Any<ILocation>()).Returns(objectOptionalBuilder);
        objectLocationBuilder.When(builder => builder.WithLocation(Arg.Any<ILocation>()))
            .Do(callInfo => copiedLocation = callInfo.Arg<ILocation>());
        objectOptionalBuilder.WithDefinition(Arg.Any<IGameObjectDefinition>()).Returns(objectOptionalBuilder);
        objectOptionalBuilder.WithRotation(Arg.Any<int>()).Returns(objectOptionalBuilder);
        objectOptionalBuilder.WithShape(Arg.Any<ShapeType>()).Returns(objectOptionalBuilder);
        objectOptionalBuilder.AsStatic().Returns(objectOptionalBuilder);
        objectOptionalBuilder.Build().Returns(_ =>
        {
            copiedObject = CreateGameObject(copiedLocation!);
            cancellation.Cancel();
            return copiedObject;
        });
        var source = Location.Create(64, 64, 0, 0);
        var destination = Location.Create(128, 64, 0, 0);
        var templateLoader = CreateTemplateLoader(regionId => new MapRegionTemplate(regionId,
            [new MapRegionTemplateObject(42, 0, 0, 0, 0, ShapeType.GroundDefault, CreateGameObject(source).Definition, true)], []));
        using var provider = CreateProvider(loadRequests, templateLoader);
        var service = CreateService(provider, new LocationBuilder(), gameObjectBuilder);
        var sourceRegion = service.GetOrCreateMapRegion(source.RegionId, source.Dimension);
        sourceRegion.MarkReady();

        service.CreateDynamicRegion(source, destination);
        var taskService = provider.GetRequiredService<IRsTaskService>();
        taskService.Tick();
        taskService.Tick();

        var destinationRegion = service.FindMapRegion(destination.RegionId, destination.Dimension);
        Assert.IsNotNull(destinationRegion);
        Assert.AreEqual(MapRegionState.Ready, destinationRegion.State);
        Assert.IsTrue(destinationRegion.IsDynamic);
    }

    [TestMethod]
    public void CreateDynamicRegion_MapsEveryPartFromTheAbsoluteSourcePart()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        loadRequests.EnsureLoadedAsync(Arg.Any<IEnumerable<IMapRegion>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var templateLoader = CreateTemplateLoader();
        using var provider = CreateProvider(loadRequests, templateLoader);
        var service = CreateService(provider);
        var source = Location.Create(3200, 3264, 0, 0);
        var destination = Location.Create(640, 640, 0, 0);
        service.GetOrCreateMapRegion(source.RegionId, source.Dimension).MarkReady();

        service.CreateDynamicRegion(source, destination);
        provider.GetRequiredService<IRsTaskService>().Tick();

        var dynamicRegion = service.FindMapRegion(destination.RegionId, destination.Dimension)!;
        Assert.AreEqual(MapRegionState.Ready, dynamicRegion.State);
        for (var z = 0; z < 4; z++)
        {
            for (var x = 0; x < 8; x++)
            {
                for (var y = 0; y < 8; y++)
                {
                    var part = dynamicRegion.GetRegionPartByLocalPart(x, y, z);
                    Assert.IsTrue(part.HasDrawSource);
                    Assert.AreEqual(50 * 8 + x, part.DrawRegionPartX);
                    Assert.AreEqual(51 * 8 + y, part.DrawRegionPartY);
                    Assert.AreEqual(z, part.DrawRegionZ);
                    Assert.AreEqual(0, part.Rotation);
                }
            }
        }

        templateLoader.Received(1).LoadAsync(source.RegionId, Arg.Any<int[]>(), Arg.Any<CancellationToken>());
        templateLoader.ReceivedWithAnyArgs(1).LoadAsync(default, default!, default);
    }

    [TestMethod]
    public void CreateDynamicRegion_ContentComesFromTheTemplateNotTheLiveSource()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        loadRequests.EnsureLoadedAsync(Arg.Any<IEnumerable<IMapRegion>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var definition = CreateSolidDefinition();
        var templateLoader = CreateTemplateLoader(regionId => new MapRegionTemplate(regionId,
            [new MapRegionTemplateObject(42, 3, 4, 0, 0, ShapeType.GroundDefault, definition, true)],
            [new BlockedTerrainTile(5, 5, 0)]));
        using var provider = CreateProvider(loadRequests, templateLoader);
        var service = CreateService(provider, new LocationBuilder(), CreateGameObjectBuilder());
        Assert.IsTrue(service.TryCreateDimension(out var dimension));
        var source = Location.Create(64, 64, 0, 0);
        var mutatedSource = service.GetOrCreateMapRegion(source.RegionId, source.Dimension);
        mutatedSource.MarkReady();
        mutatedSource.Add(new GameObject(77, Location.Create(64 + 6, 64 + 6, 0, 0), 0, ShapeType.GroundDefault, false, definition, Substitute.For<IGameObjectScript>()));
        mutatedSource.FlagCollision(1, 1, 0, CollisionFlag.WallNorth);
        var firstDestination = Location.Create(128, 64, 0, 0);
        var secondDestination = Location.Create(128, 64, 0, dimension!.Id);

        service.CreateDynamicRegion(source, firstDestination);
        provider.GetRequiredService<IRsTaskService>().Tick();
        Assert.IsTrue(service.TryRemoveMapRegion(source.RegionId, source.Dimension, mutatedSource));
        service.GetOrCreateMapRegion(source.RegionId, source.Dimension).MarkReady();
        service.CreateDynamicRegion(source, secondDestination);
        provider.GetRequiredService<IRsTaskService>().Tick();

        var first = service.FindMapRegion(firstDestination.RegionId, firstDestination.Dimension)!;
        var second = service.FindMapRegion(secondDestination.RegionId, secondDestination.Dimension)!;
        string Snapshot(IMapRegion region) => string.Join(";", region.FindAllGameObjects()
            .Select(obj => $"{obj.Id}:{obj.Location.RegionLocalX},{obj.Location.RegionLocalY},{obj.Location.Z}:{obj.Rotation}:{obj.IsStatic}")
            .OrderBy(value => value));
        CollisionFlag[] Collision(IMapRegion region) => Enumerable.Range(0, 4 * 4096)
            .Select(index => region.GetCollision((index / 64) % 64, index % 64, index / 4096)).ToArray();
        Assert.AreEqual("42:3,4,0:0:True", Snapshot(first));
        Assert.AreEqual(Snapshot(first), Snapshot(second));
        CollectionAssert.AreEqual(Collision(first), Collision(second));
        Assert.AreEqual(CollisionFlag.FloorBlock, first.GetCollision(5, 5, 0));
        Assert.AreEqual(CollisionFlag.Walkable, first.GetCollision(1, 1, 0));
    }

    [TestMethod]
    public void CreateDynamicRegion_BuildsEachObjectOnlyForItsFinalPlacement()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        loadRequests.EnsureLoadedAsync(Arg.Any<IEnumerable<IMapRegion>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var templateLoader = CreateTemplateLoader(regionId => new MapRegionTemplate(regionId,
            [new MapRegionTemplateObject(42, 3, 4, 0, 0, ShapeType.GroundDefault, CreateSolidDefinition(), true)], []));
        var script = Substitute.For<IGameObjectScript>();
        using var provider = CreateProvider(loadRequests, templateLoader);
        var service = CreateService(provider, new LocationBuilder(), CreateGameObjectBuilder(script));
        var source = Location.Create(64, 64, 0, 0);
        var destination = Location.Create(128, 64, 0, 0);
        service.GetOrCreateMapRegion(source.RegionId, source.Dimension).MarkReady();

        service.CreateDynamicRegion(source, destination);
        provider.GetRequiredService<IRsTaskService>().Tick();

        Assert.AreEqual(1, service.FindMapRegion(destination.RegionId, destination.Dimension)!.FindAllGameObjects().Count());
        script.Received(1).OnSpawn();
        script.DidNotReceive().OnDestroy();
    }

    [TestMethod]
    public async Task CreateDynamicRegion_CanceledWhileALoaderIgnoresCancellation_PublishesNothing()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var templateLoader = Substitute.For<IMapRegionTemplateLoader>();
        templateLoader.LoadAsync(Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await gate.Task; // deliberately ignores the cancellation token
                return new MapRegionTemplate(callInfo.ArgAt<int>(0), [], []);
            });
        using var provider = CreateProvider(templateLoader: templateLoader);
        var service = CreateService(provider);
        var destination = Location.Create(128, 64, 0, 0);
        var taskService = provider.GetRequiredService<IRsTaskService>();
        var handle = service.CreateDynamicRegion(Location.Create(64, 64, 0, 0), destination);
        taskService.Tick();

        handle.Cancel();
        gate.SetResult();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            taskService.Tick();
            await Task.Delay(10);
        }

        Assert.IsNull(service.FindMapRegion(destination.RegionId, destination.Dimension));
    }

    [TestMethod]
    public void CreateDynamicRegion_CanceledWhileALoaderHangs_ReleasesThePendingDestination()
    {
        var templateLoader = Substitute.For<IMapRegionTemplateLoader>();
        templateLoader.LoadAsync(Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<CancellationToken>())
            .Returns(new TaskCompletionSource<MapRegionTemplate>().Task); // never completes and ignores the token
        using var provider = CreateProvider(templateLoader: templateLoader);
        var service = CreateService(provider);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));
        var destination = Location.Create(128, 64, 0, dimension!.Id);
        var taskService = provider.GetRequiredService<IRsTaskService>();
        var handle = service.CreateDynamicRegion(Location.Create(64, 64, 0, 0), destination);
        taskService.Tick();
        Assert.IsFalse(service.TryRemoveEmptyDimension(dimension), "The creation is still pending.");

        handle.Cancel();
        taskService.Tick();
        taskService.Tick();

        Assert.IsTrue(service.TryRemoveEmptyDimension(dimension), "Cancellation must release the pending destination.");
    }

    [TestMethod]
    public void CreateDynamicRegion_FailedTemplateLoadNeverExposesAPartialDestination()
    {
        var templateLoader = Substitute.For<IMapRegionTemplateLoader>();
        templateLoader.LoadAsync(Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MapRegionTemplate>(new InvalidOperationException("template unavailable")));
        using var provider = CreateProvider(templateLoader: templateLoader);
        var service = CreateService(provider);
        var source = Location.Create(64, 64, 0, 0);
        var destination = Location.Create(128, 64, 0, 0);

        service.CreateDynamicRegion(source, destination);
        var taskService = provider.GetRequiredService<IRsTaskService>();
        taskService.Tick();
        taskService.Tick();

        Assert.IsNull(service.FindMapRegion(destination.RegionId, destination.Dimension));
        Assert.IsFalse(service.GetOrCreateMapRegion(destination.RegionId, destination.Dimension).IsDynamic);
    }

    [TestMethod]
    public void AttachCharacter_ToExistingRegionDoesNotRequestDuplicateLoad()
    {
        var loadRequests = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = CreateProvider(loadRequests);
        var service = CreateService(provider);
        var location = Location.Create(64, 64, 0, 0);
        var region = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);
        var character = Substitute.For<ICharacter>();
        character.Location.Returns(location);
        character.Index.Returns(1);

        var attachedRegion = service.AttachCharacter(character);

        Assert.AreSame(region, attachedRegion);
        loadRequests.Received(1).RequestLoad(region);
    }

    [TestMethod]
    public void TryRemoveIdleMapRegion_FailsAfterRegionResumes()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0);
        region.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(region));

        var resumed = service.GetOrCreateMapRegion(1, 0);

        Assert.AreSame(region, resumed);
        Assert.IsFalse(service.TryRemoveIdleMapRegion(region.Id, 0, region));
    }

    [TestMethod]
    public void TryRemoveIdleMapRegion_WinsBeforeResumeAndForcesFreshCreation()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0);
        region.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(region));

        Assert.IsTrue(service.TryRemoveIdleMapRegion(region.Id, 0, region));
        var replacement = service.GetOrCreateMapRegion(region.Id, 0);

        Assert.AreNotSame(region, replacement);
        Assert.AreSame(replacement, service.FindMapRegion(region.Id, 0));
        Assert.IsFalse(service.FindIdleRegionsByDimension(0).Contains(region));
    }

    [TestMethod]
    public void TryRemoveIdleMapRegion_ReleasesExactOwnerAfterRemoval()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));
        var region = service.GetOrCreateMapRegion(1, dimension!.Id);
        region.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(region));

        Assert.IsTrue(service.TryRemoveIdleMapRegion(region.Id, dimension.Id, region));
        Assert.IsTrue(service.TryRemoveEmptyDimension(dimension));

        region.Destroy();
    }

    [TestMethod]
    public async Task TrySuspendMapRegion_ConcurrentResumeDoesNotCreateDuplicateResidency()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var region = service.GetOrCreateMapRegion(1, 0);
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
            return service.GetOrCreateMapRegion(region.Id, 0);
        });

        await Task.WhenAll(suspend, resume);
        var residentRegions = service.FindRegionsByDimension(0)
            .Concat(service.FindIdleRegionsByDimension(0))
            .ToArray();

        Assert.AreEqual(1, residentRegions.Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.AreSame(region, residentRegions.Single());
    }

    [TestMethod]
    public void StaleRegionOperations_CannotAffectReplacementActiveRegion()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var staleRegion = service.GetOrCreateMapRegion(1, 0);
        Assert.IsTrue(service.TryRemoveMapRegion(staleRegion.Id, 0, staleRegion));
        var currentRegion = service.GetOrCreateMapRegion(1, 0);

        Assert.IsFalse(service.TrySuspendMapRegion(staleRegion));
        Assert.IsFalse(service.TryRemoveIdleMapRegion(staleRegion.Id, 0, staleRegion));
        Assert.IsFalse(service.TryRemoveMapRegion(staleRegion.Id, 0, staleRegion));
        Assert.AreSame(currentRegion, service.FindMapRegion(1, 0));
    }

    [TestMethod]
    public void NewRegion_StartsInitializing()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);

        var region = service.GetOrCreateMapRegion(Location.Create(67, 69, 0, 0).RegionId, 0);

        Assert.AreEqual(MapRegionState.Initializing, region.State);
    }

    [TestMethod]
    public void DimensionResidencyReads_AreStableSnapshots()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var dimension = service.FindAllDimensions().Single();
        var region = service.GetOrCreateMapRegion(1, dimension.Id);
        var snapshot = service.FindRegionsByDimension(dimension.Id);

        Assert.IsTrue(snapshot.Contains(region));
        Assert.IsTrue(service.TryRemoveMapRegion(region.Id, dimension.Id, region));
        Assert.IsTrue(snapshot.Contains(region));
        Assert.IsFalse(service.FindRegionsByDimension(dimension.Id).Contains(region));
    }

    [TestMethod]
    public void TryRemoveEmptyDimension_EnforcesExactOwnerAndEmptyResidency()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));

        Assert.IsFalse(service.TryRemoveEmptyDimension(new Dimension(dimension!.Id)));

        var region = service.GetOrCreateMapRegion(1, dimension.Id);
        Assert.IsFalse(service.TryRemoveEmptyDimension(dimension));
        Assert.IsTrue(service.TryRemoveMapRegion(region.Id, dimension.Id, region));
        Assert.IsTrue(service.TryRemoveEmptyDimension(dimension));
        Assert.IsFalse(service.FindAllDimensions().Any(item => item.Id == dimension.Id));
    }

    [TestMethod]
    public void RegionLifecycle_OnlyAllowsInitializingToTerminalState()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        var readyRegion = service.GetOrCreateMapRegion(1, 0);
        var discardedRegion = service.GetOrCreateMapRegion(2, 0);

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
        var currentRegion = service.GetOrCreateMapRegion(location.RegionId, location.Dimension);
        var otherRegion = service.GetOrCreateMapRegion(2, 0);

        Assert.IsTrue(service.IsCurrentMapRegion(currentRegion.Id, location.Dimension, currentRegion));
        Assert.IsFalse(service.IsCurrentMapRegion(currentRegion.Id, location.Dimension, otherRegion));
    }

    [TestMethod]
    public void GetClippingFlag_ReadsTheInstanceRegionInsteadOfTheGlobalRegion()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));
        var global = Location.Create(67, 69, 0, 0);
        var instance = Location.Create(67, 69, 0, dimension!.Id);
        var globalRegion = service.GetOrCreateMapRegion(global.RegionId, global.Dimension);
        var instanceRegion = service.GetOrCreateMapRegion(instance.RegionId, instance.Dimension);
        globalRegion.FlagCollision(global.RegionLocalX, global.RegionLocalY, global.Z, CollisionFlag.FloorBlock);
        globalRegion.MarkReady();
        instanceRegion.MarkReady();

        Assert.AreEqual(CollisionFlag.FloorBlock, service.GetClippingFlag(global.X, global.Y, global.Z));
        Assert.AreEqual(CollisionFlag.Walkable, service.GetClippingFlag(instance.X, instance.Y, instance.Z, instance.Dimension));
        Assert.IsFalse(service.IsAccessible(global));
        Assert.IsTrue(service.IsAccessible(instance));
    }

    [TestMethod]
    public void GetClippingFlag_MissingInstanceRegionOrDimension_BlocksWithoutCreatingARegion()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));
        var location = Location.Create(67, 69, 0, dimension!.Id);

        Assert.AreEqual(CollisionFlag.FloorBlock | CollisionFlag.ObjectBlock, service.GetClippingFlag(location.X, location.Y, location.Z, location.Dimension));
        Assert.AreEqual(CollisionFlag.FloorBlock | CollisionFlag.ObjectBlock, service.GetClippingFlag(location.X, location.Y, location.Z, 200));
        Assert.AreEqual(CollisionFlag.FloorBlock | CollisionFlag.ObjectBlock, service.GetClippingFlag(location.X, location.Y, location.Z, MapRegionService.MaxDimensions));
        Assert.AreEqual(CollisionFlag.FloorBlock | CollisionFlag.ObjectBlock, service.GetClippingFlag(location.X, location.Y, location.Z, -1));
        Assert.IsNull(service.FindMapRegion(location.RegionId, location.Dimension));
    }

    [TestMethod]
    public void ProjectileFind_ThroughMissingOrNotReadyRegion_IsBlocked()
    {
        using var provider = CreateProvider();
        var service = CreateService(provider);
        Assert.IsTrue(service.TryCreateDimension(out var dimension));
        var pathFinder = new ProjectilePathFinder(service);
        var from = Location.Create(60, 10, 0, dimension!.Id);
        var to = Location.Create(70, 10, 0, dimension.Id);

        Assert.IsFalse(pathFinder.Find(from, 1, to, 1, 1, 0, 0, 0, true).Successful);

        service.GetOrCreateMapRegion(from.RegionId, from.Dimension).MarkReady();
        service.GetOrCreateMapRegion(to.RegionId, to.Dimension);

        Assert.IsFalse(pathFinder.Find(from, 1, to, 1, 1, 0, 0, 0, true).Successful);

        service.GetOrCreateMapRegion(to.RegionId, to.Dimension).MarkReady();

        Assert.IsTrue(pathFinder.Find(from, 1, to, 1, 1, 0, 0, 0, true).Successful);
    }

    private static readonly ILocation StandardSource = Location.Create(3200, 3264, 0, 0);

    /// <summary>
    ///     A dynamic creation whose source templates are held back by <see cref="Gate"/>, one tick after it started.
    /// </summary>
    private sealed record PendingCreation(
        ServiceProvider Provider,
        MapRegionService Service,
        IRsTaskService Tasks,
        IMapRegionTemplateLoader Loader,
        TaskCompletionSource Gate,
        ILocation Destination) : IDisposable
    {
        public void Dispose() => Provider.Dispose();
    }

    private static PendingCreation StartPendingCreation(Func<MapRegionService, (ILocation Source, ILocation Destination)> locations)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loader = CreateGatedTemplateLoader(gate);
        var provider = CreateProvider(templateLoader: loader);
        var service = CreateService(provider);
        var (source, destination) = locations(service);
        var tasks = provider.GetRequiredService<IRsTaskService>();
        service.CreateDynamicRegion(source, destination);
        tasks.Tick();
        return new PendingCreation(provider, service, tasks, loader, gate, destination);
    }

    private static async Task<IMapRegion> WaitForRegionAsync(MapRegionService service, IRsTaskService taskService, ILocation location)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            taskService.Tick();
            if (service.FindMapRegion(location.RegionId, location.Dimension) is { } region)
            {
                return region;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException($"Region {location.RegionId} was never published.");
    }

    private static ServiceProvider CreateProvider(
        IMapRegionLoadScheduler? loadRequests = null,
        IMapRegionTemplateLoader? templateLoader = null) => new ServiceCollection()
        .AddSingleton(Substitute.For<INpcService>())
        .AddSingleton<IEntityStore, EntityStore>()
        .AddSingleton(loadRequests ?? Substitute.For<IMapRegionLoadScheduler>())
        .AddSingleton<IRsTaskService>(_ => new RsTaskService(Substitute.For<ILogger<RsTaskService>>()))
        .AddSingleton(templateLoader ?? CreateTemplateLoader())
        .BuildServiceProvider();

    private static IMapRegionTemplateLoader CreateTemplateLoader(Func<int, MapRegionTemplate>? templates = null)
    {
        var loader = Substitute.For<IMapRegionTemplateLoader>();
        loader.LoadAsync(Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var regionId = callInfo.ArgAt<int>(0);
                return Task.FromResult(templates?.Invoke(regionId) ?? new MapRegionTemplate(regionId, [], []));
            });
        return loader;
    }

    /// <summary>
    ///     A template loader that returns empty templates once <paramref name="gate"/> is released.
    /// </summary>
    private static IMapRegionTemplateLoader CreateGatedTemplateLoader(TaskCompletionSource gate)
    {
        var loader = Substitute.For<IMapRegionTemplateLoader>();
        loader.LoadAsync(Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await gate.Task.WaitAsync(callInfo.Arg<CancellationToken>());
                return new MapRegionTemplate(callInfo.ArgAt<int>(0), [], []);
            });
        return loader;
    }

    private static GameObjectBuilder CreateGameObjectBuilder(IGameObjectScript? script = null)
    {
        var scriptProvider = Substitute.For<Hagalaz.Game.Abstractions.Providers.IGameObjectScriptProvider>();
        scriptProvider.GetGameObjectScriptTypeById(Arg.Any<int>()).Returns(typeof(IGameObjectScript));
        var services = new ServiceCollection().AddSingleton(_ => script ?? Substitute.For<IGameObjectScript>()).BuildServiceProvider();
        return new GameObjectBuilder(services, scriptProvider);
    }

    private static IGameObjectDefinition CreateSolidDefinition()
    {
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.SizeX.Returns(1);
        definition.SizeY.Returns(1);
        definition.ClipType.Returns(1);
        definition.Solid.Returns(true);
        return definition;
    }

    private static MapRegionService CreateService(IServiceProvider provider) => CreateService(provider, new LocationBuilder());

    private static MapRegionService CreateService(
        IServiceProvider provider,
        ILocationBuilder locationBuilder,
        IGameObjectBuilder? gameObjectBuilder = null) => new(
        provider,
        locationBuilder,
        gameObjectBuilder ?? Substitute.For<IGameObjectBuilder>(),
        Substitute.For<IGroundItemBuilder>(),
        Substitute.For<ILogger<MapRegionService>>(),
        Substitute.For<IMapper>(),
        provider.GetRequiredService<IMapRegionLoadScheduler>(),
        provider.GetRequiredService<IRsTaskService>(),
        provider.GetRequiredService<IEntityStore>());

    private static GameObject CreateGameObject(ILocation location)
    {
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.SizeX.Returns(1);
        definition.SizeY.Returns(1);
        return new GameObject(
            42,
            location,
            0,
            ShapeType.GroundDefault,
            false,
            definition,
            Substitute.For<IGameObjectScript>());
    }

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
