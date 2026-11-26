using System.Diagnostics;
using System.Text.Json;
using Hagalaz.Cache.Maps;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types.Factories;
using Hagalaz.Cache.Types.Providers;
using Hagalaz.Game.Abstractions.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq.Expressions;
using AutoMapper;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Profiles;
using Hagalaz.Services.GameWorld.Model.Items;
using Hagalaz.Services.GameWorld.Model.Maps.GameObjects;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
[DoNotParallelize]
public sealed class MapRegionLoaderTests
{
    [TestMethod]
    public async Task LoadAsync_EmitsPerRegionActivityWithBoundedLoadDetails()
    {
        var region = CreateRegion();
        var regionId = Random.Shared.Next(1_000_000, int.MaxValue);
        region.Id.Returns(regionId);
        var stoppedActivities = new List<Activity>();
        using var measurements = new MeterListenerSpy("Hagalaz.Services.GameWorld");
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Hagalaz.Services.GameWorld",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.GetTagItem("map.region.id") is int stoppedRegionId && stoppedRegionId == regionId)
                    stoppedActivities.Add(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);
        var mapProvider = CreateMapProvider();
        var fixture = CreateLoader(mapProvider: mapProvider);

        await fixture.Loader.LoadAsync(region);

        Assert.AreEqual(1, stoppedActivities.Count);
        var stoppedActivity = stoppedActivities[0];
        Assert.AreEqual("Hagalaz.Services.GameWorld.MapRegionLoader.Load", stoppedActivity.OperationName);
        Assert.AreEqual(region.Id, stoppedActivity.GetTagItem("map.region.id"));
        Assert.AreEqual(region.BaseLocation.Dimension, stoppedActivity.GetTagItem("map.region.dimension"));
        Assert.AreEqual(0, stoppedActivity.GetTagItem("map.region.gameobject_placement_count"));
        Assert.AreEqual("success", stoppedActivity.GetTagItem("map.region.load.outcome"));
        Assert.AreEqual(ActivityStatusCode.Ok, stoppedActivity.Status);

        var count = measurements.Measurements.Single(measurement => measurement.InstrumentName == "hagalaz.gameworld.map.region.load");
        Assert.AreEqual(1d, count.Value);
        Assert.AreEqual("success", count.Tags["outcome"]);
        var duration = measurements.Measurements.Single(measurement => measurement.InstrumentName == "hagalaz.gameworld.map.region.load.duration");
        Assert.AreEqual("success", duration.Tags["outcome"]);
    }

    [TestMethod]
    public async Task LoadAsync_StagesStaticDecodeBeforeApplyingRegionState()
    {
        var decodeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseDecode = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var region = CreateRegion();
        var mapProvider = CreateMapProvider();
        mapProvider.DecodeRegion(Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                decodeStarted.TrySetResult();
                releaseDecode.Task.GetAwaiter().GetResult();
                return new DecodedMapRegion([], [new BlockedTerrainTile(1, 1, 0)]);
            });
        var fixture = CreateLoader(mapProvider: mapProvider);

        var loadTask = Task.Run(() => fixture.Loader.LoadAsync(region));
        await decodeStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.AreEqual(MapRegionState.Initializing, region.State);
        region.DidNotReceive().FlagCollision(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CollisionFlag>());

        releaseDecode.TrySetResult();
        await loadTask;

        Assert.AreEqual(MapRegionState.Ready, region.State);
        await fixture.GameObjectService.DidNotReceive().FindGameObjectDefinitionsByIdsAsync(
            Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>());
        region.Received(1).FlagCollision(1, 1, 0, CollisionFlag.FloorBlock);
        region.Received(1).MarkReady();
    }

    [TestMethod]
    public async Task LoadAsync_WhenStaticDecodeFails_DoesNotApplyStagedDataOrRegisterNpcs()
    {
        var stoppedActivities = new List<Activity>();
        using var measurements = new MeterListenerSpy("Hagalaz.Services.GameWorld");
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Hagalaz.Services.GameWorld",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == "Hagalaz.Services.GameWorld.MapRegionLoader.Load")
                    stoppedActivities.Add(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);

        var region = CreateRegion();
        var gameObject = Substitute.For<IGameObject>();
        var gameObjectBuilder = ConfigureStaticGameObjectBuilder(gameObject);
        var mapProvider = CreateMapProvider();
        var failure = new InvalidDataException("corrupt map data");
        mapProvider.DecodeRegion(Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw failure);
        var fixture = CreateLoader(mapProvider: mapProvider, gameObjectBuilder: gameObjectBuilder);

        var actual = await Assert.ThrowsExactlyAsync<InvalidDataException>(() => fixture.Loader.LoadAsync(region));

        Assert.AreSame(failure, actual);
        Assert.AreEqual(MapRegionState.Discarded, region.State);
        var failedActivity = stoppedActivities.Single();
        Assert.AreEqual("failure", failedActivity.GetTagItem("map.region.load.outcome"));
        Assert.AreEqual(ActivityStatusCode.Error, failedActivity.Status);
        Assert.AreEqual(typeof(InvalidDataException).FullName, failedActivity.GetTagItem("error.type"));
        var loadCount = measurements.Measurements.Single(measurement => measurement.InstrumentName == "hagalaz.gameworld.map.region.load");
        Assert.AreEqual("failure", loadCount.Tags["outcome"]);
        var loadDuration = measurements.Measurements.Single(measurement => measurement.InstrumentName == "hagalaz.gameworld.map.region.load.duration");
        Assert.AreEqual("failure", loadDuration.Tags["outcome"]);
        region.DidNotReceive().FlagCollision(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CollisionFlag>());
        region.DidNotReceive().Add(gameObject);
        await fixture.NpcService.DidNotReceive().RegisterAsync(Arg.Any<INpc>());
        fixture.RegionService.Received(1).TryRemoveMapRegion(region.Id, region.BaseLocation.Dimension, region);
    }

    [TestMethod]
    public async Task LoadAsync_WhenObjectConstructionFails_CleansPreviouslyCreatedObjectAndDiscardsRegion()
    {
        var region = CreateRegion();
        var firstObject = Substitute.For<IGameObject>();
        var constructionFailure = new InvalidOperationException("second object could not be constructed");
        var gameObjectBuilder = ConfigureStaticGameObjectBuilderWithFailure(firstObject, constructionFailure);
        var mapProvider = CreateMapProvider();
        var decodedRegion = new DecodedMapRegion(
            [CreateMapObject(100, 0, 0, 3, 3, 0), CreateMapObject(101, 0, 0, 4, 4, 0)],
            []);
        mapProvider.DecodeRegion(Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<CancellationToken>())
            .Returns(decodedRegion);
        var fixture = CreateLoader(mapProvider: mapProvider, gameObjectBuilder: gameObjectBuilder);

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => fixture.Loader.LoadAsync(region));

        Assert.AreSame(constructionFailure, actual);
        Assert.AreEqual(MapRegionState.Discarded, region.State);
        await fixture.GameObjectService.Received(1).FindGameObjectDefinitionsByIdsAsync(
            Arg.Is<IEnumerable<int>>(ids => ids.OrderBy(id => id).SequenceEqual(new[] { 100, 101 })),
            Arg.Any<CancellationToken>());
        firstObject.Received(1).Destroy();
        region.DidNotReceive().Add(firstObject);
        region.DidNotReceive().FlagCollision(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CollisionFlag>());
        fixture.RegionService.Received(1).TryRemoveMapRegion(region.Id, region.BaseLocation.Dimension, region);
    }

    [TestMethod]
    public async Task LoadAsync_WhenSourceQueryFails_DiscardsRegionWithoutMutatingIt()
    {
        var region = CreateRegion();
        var failure = new InvalidOperationException("spawn database unavailable");
        var fixture = CreateLoader(itemSourceFailure: failure);

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => fixture.Loader.LoadAsync(region));

        Assert.AreSame(failure, actual);
        Assert.AreEqual(MapRegionState.Discarded, region.State);
        region.DidNotReceive().FlagCollision(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CollisionFlag>());
        region.DidNotReceive().Add(Arg.Any<IGameObject>());
        region.DidNotReceive().Add(Arg.Any<Hagalaz.Game.Abstractions.Model.Items.IGroundItem>());
        fixture.RegionService.Received(1).TryRemoveMapRegion(region.Id, region.BaseLocation.Dimension, region);
    }

    [TestMethod]
    public async Task LoadAsync_ResolvesDistinctStaticAndDatabaseSpawnIdsInOneRegionBatch()
    {
        var region = CreateRegion();
        var mapProvider = CreateMapProvider();
        var decodedRegion = new DecodedMapRegion(
            [
                CreateMapObject(100, 0, 0, 1, 1, 0),
                CreateMapObject(100, 0, 0, 2, 2, 0),
                CreateMapObject(101, 0, 0, 3, 3, 0)
            ],
            []);
        mapProvider.DecodeRegion(Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<CancellationToken>())
            .Returns(decodedRegion);
        var definitions = new Dictionary<int, IGameObjectDefinition>
        {
            [100] = Substitute.For<IGameObjectDefinition>(),
            [101] = Substitute.For<IGameObjectDefinition>(),
            [200] = Substitute.For<IGameObjectDefinition>()
        };
        var gameObjectService = Substitute.For<IGameObjectService>();
        gameObjectService.FindGameObjectDefinitionsByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<int, IGameObjectDefinition>>(definitions));
        var gameObjectBuilder = Substitute.For<IGameObjectBuilder>();
        var idBuilder = Substitute.For<IGameObjectId>();
        var locationBuilder = Substitute.For<IGameObjectLocation>();
        var optionalBuilder = Substitute.For<IGameObjectOptional>();
        gameObjectBuilder.Create().Returns(idBuilder);
        idBuilder.WithId(Arg.Any<int>()).Returns(locationBuilder);
        locationBuilder.WithLocation(Arg.Any<ILocation>()).Returns(optionalBuilder);
        optionalBuilder.WithDefinition(Arg.Any<IGameObjectDefinition>()).Returns(optionalBuilder);
        optionalBuilder.WithRotation(Arg.Any<int>()).Returns(optionalBuilder);
        optionalBuilder.WithShape(Arg.Any<ShapeType>()).Returns(optionalBuilder);
        optionalBuilder.AsStatic().Returns(optionalBuilder);
        optionalBuilder.Build().Returns(Substitute.For<IGameObject>());
        var fixture = CreateLoader(
            mapProvider: mapProvider,
            gameObjectBuilder: gameObjectBuilder,
            gameObjectService: gameObjectService,
            objectSpawns: [new GameobjectSpawn { GameobjectId = 200, CoordX = 10, CoordY = 10 }]);

        await fixture.Loader.LoadAsync(region);

        await fixture.GameObjectService.Received(1).FindGameObjectDefinitionsByIdsAsync(
            Arg.Is<IEnumerable<int>>(ids => ids.OrderBy(id => id).SequenceEqual(new[] { 100, 101, 200 })),
            Arg.Any<CancellationToken>());
        optionalBuilder.Received(2).WithDefinition(definitions[100]);
        optionalBuilder.Received(1).WithDefinition(definitions[101]);
        optionalBuilder.Received(1).WithDefinition(definitions[200]);
    }

    [TestMethod]
    public async Task TemplateLoader_ReturnsCacheObjectsDatabaseSpawnsAndBlockedTilesInRegionLocalCoordinates()
    {
        const int regionId = (50 << 8) | 51;
        var mapProvider = CreateMapProvider(new DecodedMapRegion(
            [CreateMapObject(100, 10, 3, 5, 6, 1)],
            [new BlockedTerrainTile(7, 8, 2)]));
        var objectRepository = Substitute.For<IGameObjectSpawnRepository>();
        objectRepository.FindByBounds(3200, 3264, 3263, 3327)
            .Returns(new TestAsyncEnumerable<GameobjectSpawn>(
                [new GameobjectSpawn { GameobjectId = 200, CoordX = 3210, CoordY = 3270, CoordZ = 2, Face = 1, Type = 22 }]));
        var definitions = new Dictionary<int, IGameObjectDefinition>
        {
            [100] = Substitute.For<IGameObjectDefinition>(),
            [200] = Substitute.For<IGameObjectDefinition>()
        };
        var gameObjectService = Substitute.For<IGameObjectService>();
        gameObjectService.FindGameObjectDefinitionsByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<int, IGameObjectDefinition>>(definitions));
        var xtea = new[] { 1, 2, 3, 4 };

        var template = await new MapRegionTemplateLoader(objectRepository, gameObjectService, mapProvider).LoadAsync(regionId, xtea);

        mapProvider.Received(1).DecodeRegion(regionId, xtea, Arg.Any<CancellationToken>());
        Assert.AreEqual(regionId, template.RegionId);
        CollectionAssert.AreEqual(new[] { new BlockedTerrainTile(7, 8, 2) }, template.BlockedTiles.ToArray());
        Assert.AreEqual(2, template.Objects.Count);
        Assert.AreEqual(new MapRegionTemplateObject(100, 5, 6, 1, 3, (ShapeType)10, definitions[100], true), template.Objects[0]);
        Assert.AreEqual(new MapRegionTemplateObject(200, 10, 6, 2, 1, (ShapeType)22, definitions[200], false), template.Objects[1]);
    }

    [TestMethod]
    public async Task LoadAsync_AddsPlaneZeroObjectUnderABridgeWithoutCollision()
    {
        var region = CreateRegion();
        var gameObject = Substitute.For<IGameObject>();
        var terrain = new sbyte[4, 64, 64];
        terrain[1, 3, 3] = 0x2;
        var rawObject = CreateMapObject(100, 10, 0, 3, 3, 0);
        var mapProvider = CreateMapProvider(new DecodedMapRegion([], [], [rawObject], terrain));
        var fixture = CreateLoader(mapProvider: mapProvider, gameObjectBuilder: ConfigureStaticGameObjectBuilder(gameObject));

        await fixture.Loader.LoadAsync(region);

        region.Received(1).Add(gameObject, false);
        region.DidNotReceive().Add(gameObject);
    }

    [TestMethod]
    public async Task LoadAsync_WhenRegionIsDiscarded_RejectsTheStaleInstance()
    {
        var region = CreateRegion();
        region.MarkDiscarded();
        var fixture = CreateLoader();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => fixture.Loader.LoadAsync(region));

        await fixture.NpcService.DidNotReceive().RegisterAsync(Arg.Any<INpc>());
        fixture.RegionService.DidNotReceive().TryRemoveMapRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IMapRegion>());
    }

    [TestMethod]
    public async Task LoadAsync_WhenNpcRegistrationFails_DiscardsRegionAndCleansUpRegisteredNpcs()
    {
        var region = CreateRegion();
        var npcA = CreateNpc(1);
        var npcB = CreateNpc(2);
        var npcC = CreateNpc(3);
        var fixture = CreateLoader(
            npcSpawns: CreateNpcSpawns(1, 2, 3));
        ConfigureNpcBuilder(fixture.NpcBuilder, (1, npcA), (2, npcB), (3, npcC));
        var registrationFailure = new InvalidOperationException("NPC B script failed");
        fixture.NpcService.RegisterAsync(npcA).Returns(_ =>
        {
            region.Add(npcA);
            return Task.CompletedTask;
        });
        fixture.NpcService.RegisterAsync(npcB).Returns(Task.FromException(registrationFailure));
        fixture.NpcService.RegisterAsync(npcC).Returns(_ =>
        {
            region.Add(npcC);
            return Task.CompletedTask;
        });

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => fixture.Loader.LoadAsync(region));

        Assert.AreSame(registrationFailure, actual);
        Assert.AreEqual(MapRegionState.Discarded, region.State);
        region.Received(1).Add(npcA);
        region.DidNotReceive().Add(npcB);
        region.DidNotReceive().Add(npcC);
        await fixture.NpcService.Received(1).RegisterAsync(npcA);
        await fixture.NpcService.Received(1).RegisterAsync(npcB);
        await fixture.NpcService.DidNotReceive().RegisterAsync(npcC);
        await fixture.NpcService.Received(1).UnregisterAsync(npcA);
        fixture.RegionService.Received(1).TryRemoveMapRegion(region.Id, region.BaseLocation.Dimension, region);
    }

    [TestMethod]
    public async Task LoadAsync_WhenNpcConstructionFails_DiscardsRegionAndCleansUpRegisteredNpcs()
    {
        var region = CreateRegion();
        var npcA = CreateNpc(1);
        var fixture = CreateLoader(
            npcSpawns: CreateNpcSpawns(1, 2, 3));
        var constructionFailure = new InvalidOperationException("NPC B could not be constructed");
        var idBuilders = Enumerable.Range(0, 3).Select(_ => Substitute.For<INpcId>()).ToArray();
        fixture.NpcBuilder.Create().Returns(idBuilders[0], idBuilders.Skip(1).ToArray());
        var locationA = Substitute.For<INpcLocation>();
        var optionalA = Substitute.For<INpcOptional>();
        idBuilders[0].WithId(1).Returns(locationA);
        locationA.WithLocation(Arg.Any<ILocation>()).Returns(optionalA);
        optionalA.WithMinimumBounds(Arg.Any<ILocation>()).Returns(optionalA);
        optionalA.WithMaximumBounds(Arg.Any<ILocation>()).Returns(optionalA);
        optionalA.WithFaceDirection(Arg.Any<DirectionFlag>()).Returns(optionalA);
        optionalA.Build().Returns(npcA);
        idBuilders[1].WithId(2).Returns(_ => throw constructionFailure);
        fixture.NpcService.RegisterAsync(npcA).Returns(_ =>
        {
            region.Add(npcA);
            return Task.CompletedTask;
        });

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => fixture.Loader.LoadAsync(region));

        Assert.AreSame(constructionFailure, actual);
        Assert.AreEqual(MapRegionState.Discarded, region.State);
        region.Received(1).Add(npcA);
        await fixture.NpcService.Received(1).RegisterAsync(npcA);
        await fixture.NpcService.Received(1).UnregisterAsync(npcA);
        idBuilders[2].DidNotReceive().WithId(3);
        fixture.RegionService.Received(1).TryRemoveMapRegion(region.Id, region.BaseLocation.Dimension, region);
    }

    [TestMethod]
    public async Task LoadAsync_WhenCanceledAfterNpcRegistration_UnregistersRegisteredNpcsAndDiscardsRegion()
    {
        var stoppedActivities = new List<Activity>();
        using var measurements = new MeterListenerSpy("Hagalaz.Services.GameWorld");
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Hagalaz.Services.GameWorld",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == "Hagalaz.Services.GameWorld.MapRegionLoader.Load")
                    stoppedActivities.Add(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);
        using var cancellation = new CancellationTokenSource();
        var region = CreateRegion();
        var npcA = CreateNpc(1);
        var npcB = CreateNpc(2);
        var fixture = CreateLoader(
            npcSpawns: CreateNpcSpawns(1, 2));
        ConfigureNpcBuilder(fixture.NpcBuilder, (1, npcA), (2, npcB));
        fixture.NpcService.RegisterAsync(npcA).Returns(_ =>
        {
            region.Add(npcA);
            cancellation.Cancel();
            return Task.CompletedTask;
        });
        fixture.NpcService.UnregisterAsync(npcA).Returns(Task.CompletedTask);

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => fixture.Loader.LoadAsync(region, cancellation.Token));

        Assert.AreEqual(MapRegionState.Discarded, region.State);
        var cancelledActivity = stoppedActivities.Single();
        Assert.AreEqual("cancelled", cancelledActivity.GetTagItem("map.region.load.outcome"));
        Assert.AreNotEqual(ActivityStatusCode.Error, cancelledActivity.Status);
        Assert.AreEqual("cancelled", measurements.Measurements.Single(measurement =>
            measurement.InstrumentName == "hagalaz.gameworld.map.region.load").Tags["outcome"]);
        await fixture.NpcService.Received(1).UnregisterAsync(npcA);
        await fixture.NpcService.DidNotReceive().UnregisterAsync(npcB);
        fixture.RegionService.Received(1).TryRemoveMapRegion(region.Id, region.BaseLocation.Dimension, region);
    }

    [TestMethod]
    public async Task LoadAsync_WhenCleanupOfRegisteredNpcsFails_ContinuesCleanupAndPreservesPrimaryFailure()
    {
        var fatalFailure = new InvalidOperationException("readiness publication failed");
        var region = CreateRegion(fatalFailure);
        var npcA = CreateNpc(1);
        var npcB = CreateNpc(2);
        var fixture = CreateLoader(
            npcSpawns: CreateNpcSpawns(1, 2));
        ConfigureNpcBuilder(fixture.NpcBuilder, (1, npcA), (2, npcB));
        fixture.NpcService.RegisterAsync(npcA).Returns(_ =>
        {
            region.Add(npcA);
            return Task.CompletedTask;
        });
        fixture.NpcService.RegisterAsync(npcB).Returns(_ =>
        {
            region.Add(npcB);
            return Task.CompletedTask;
        });
        var cleanupFailure = new InvalidOperationException("NPC cleanup failed");
        fixture.NpcService.UnregisterAsync(npcA).Returns(Task.FromException(cleanupFailure));
        fixture.NpcService.UnregisterAsync(npcB).Returns(Task.CompletedTask);

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => fixture.Loader.LoadAsync(region));

        Assert.AreSame(fatalFailure, actual);
        await fixture.NpcService.Received(1).UnregisterAsync(npcA);
        await fixture.NpcService.Received(1).UnregisterAsync(npcB);
        fixture.RegionService.Received(1).TryRemoveMapRegion(region.Id, region.BaseLocation.Dimension, region);
    }

    [TestMethod]
    public async Task LoadAsync_WhenReadinessPublicationFails_RollsBackAppliedObjectsAndItemsWithoutRegionDestruction()
    {
        var fatalFailure = new InvalidOperationException("readiness publication failed");
        var region = CreateRegion(fatalFailure);
        var gameObject = Substitute.For<IGameObject>();
        gameObject.IsStatic.Returns(true);
        var gameObjectBuilder = ConfigureStaticGameObjectBuilder(gameObject);
        var mapProvider = CreateMapProvider(new DecodedMapRegion([CreateMapObject(100, 0, 0, 3, 3, 0)], []));
        var item = Substitute.For<Hagalaz.Game.Abstractions.Model.Items.IGroundItem>();
        item.Location.Returns(Location.Create(65, 65, 0, 0));
        var groundItemBuilder = Substitute.For<IGroundItemBuilder>();
        var onGround = Substitute.For<IGroundItemOnGround>();
        var itemLocation = Substitute.For<IGroundItemLocation>();
        var itemOptional = Substitute.For<IGroundItemOptional>();
        groundItemBuilder.Create().Returns(onGround);
        onGround.WithItem(Arg.Any<Func<Hagalaz.Game.Abstractions.Builders.Item.IItemBuilder, Hagalaz.Game.Abstractions.Builders.Item.IItemBuild>>())
            .Returns(itemLocation);
        itemLocation.WithLocation(Arg.Any<ILocation>()).Returns(itemOptional);
        itemOptional.WithRespawnTicks(Arg.Any<int>()).Returns(itemOptional);
        itemOptional.Build().Returns(item);
        var fixture = CreateLoader(
            mapProvider: mapProvider,
            gameObjectBuilder: gameObjectBuilder,
            groundItemBuilder: groundItemBuilder,
            itemSpawns: [new ItemSpawn { ItemId = 995, Count = 1, CoordX = 65, CoordY = 65 }]);

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => fixture.Loader.LoadAsync(region));

        Assert.AreSame(fatalFailure, actual);
        gameObject.Received(1).Destroy();
        item.Received(1).Destroy();
        region.DidNotReceive().Destroy();
        fixture.RegionService.Received(1).TryRemoveMapRegion(region.Id, region.BaseLocation.Dimension, region);
    }

    [TestMethod]
    public async Task LoadAsync_RegistersLoadedGroundItemsAndGameObjectsWithTheEntityStore()
    {
        var store = new EntityStore();
        var regionService = Substitute.For<IMapRegionService>();
        var region = new MapRegion(
            Location.Create(64, 64, 0, 0),
            new int[4],
            Substitute.For<INpcService>(),
            regionService,
            Substitute.For<IGameObjectBuilder>(),
            Substitute.For<IGroundItemBuilder>(),
            Substitute.For<IMapper>(),
            store);
        var location = Location.Create(65, 65, 0, 0);
        var item = new GroundItem(
            Substitute.For<IItem>(),
            location,
            null,
            0,
            0,
            regionService);
        var gameObject = CreateConcreteGameObject(Location.Create(67, 67, 0, 0));
        var mapProvider = CreateMapProvider(new DecodedMapRegion([CreateMapObject(100, 0, 0, 3, 3, 0)], []));
        var fixture = CreateLoader(
            mapProvider: mapProvider,
            gameObjectBuilder: ConfigureStaticGameObjectBuilder(gameObject),
            groundItemBuilder: ConfigureGroundItemBuilder(item),
            itemSpawns: [new ItemSpawn { ItemId = 995, Count = 1, CoordX = (short)location.X, CoordY = (short)location.Y }],
            entityStore: store,
            regionService: regionService);
        regionService.IsCurrentMapRegion(region.Id, region.BaseLocation.Dimension, region).Returns(true);

        await fixture.Loader.LoadAsync(region);

        AssertEntityResolves(store, item);
        AssertEntityResolves(store, gameObject);

        var itemHandle = item.Handle;
        var gameObjectHandle = gameObject.Handle;
        region.Destroy();

        Assert.IsFalse(store.TryResolve(itemHandle, out _));
        Assert.IsFalse(store.TryResolve(gameObjectHandle, out _));
    }

    [TestMethod]
    public async Task LoadAsync_Region12850MatchesPinnedStaticCollision()
    {
        var (region, _) = await LoadRegion12850Async();

        var actual = Collision12850(region);
        var allFlags = actual.SelectMany(plane => plane).Aggregate(0, (all, tile) => all | tile);
        Assert.IsTrue(((CollisionFlag)allFlags).HasFlag(CollisionFlag.FloorBlock | CollisionFlag.FloorDecorationBlock | CollisionFlag.ObjectTile |
            CollisionFlag.WallWest | CollisionFlag.ObjectBlock | CollisionFlag.BlockedWest));
        AssertPinnedCollision12850(actual);
    }

    [TestMethod]
    public async Task MakeStandard_AfterDynamicEditsRestoresRegion12850()
    {
        var (region, template) = await LoadRegion12850Async();
        string[] Snapshot() => region.FindAllGameObjects()
            .Select(obj => $"{obj.Id}:{obj.Location.X},{obj.Location.Y},{obj.Location.Z}:{obj.Rotation}:{obj.ShapeType}:{obj.IsStatic}")
            .ToArray();
        var loadedObjects = Snapshot();
        var sourcePartX = region.LocalPartXToPartX(3);
        var sourcePartY = region.LocalPartYToPartY(3);

        var templates = new Dictionary<int, MapRegionTemplate> { [template.RegionId] = template };
        region.MakeDynamic();
        region.WriteBlockByIndex(0, 0, 0, sourcePartX, sourcePartY, 0, templates);
        region.RotateBlockByIndex(3, 3, 0, 1, templates);
        region.DeleteBlockByIndex(4, 4, 0, templates);
        CollectionAssert.AreNotEquivalent(loadedObjects, Snapshot());

        region.MakeStandard(template);

        Assert.IsFalse(region.IsDynamic);
        CollectionAssert.AreEquivalent(loadedObjects, Snapshot());
        AssertPinnedCollision12850(Collision12850(region));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public async Task DynamicChunks_FollowClientBridgePlanesForRegion12850(int rotation)
    {
        var (_, template) = await LoadRegion12850Async();
        var (chunkX, chunkY) = FindBridgeChunk12850(template);
        const int localPartX = 2;
        const int localPartY = 3;
        // Destination plane 0 and 1 copy source plane 1 (plane 1 supplies the bridge flags);
        // destination plane 2 copies source plane 0.
        (int Z, int SourceZ)[] mappings = [(0, 1), (1, 1), (2, 0)];
        var scriptProvider = Substitute.For<IGameObjectScriptProvider>();
        scriptProvider.GetGameObjectScriptTypeById(Arg.Any<int>()).Returns(typeof(IGameObjectScript));
        var services = new ServiceCollection().AddSingleton(Substitute.For<IGameObjectScript>()).BuildServiceProvider();
        var regionService = Substitute.For<IMapRegionService>();
        var destination = new MapRegion(Location.Create(100 * 64, 90 * 64, 0), [],
            Substitute.For<INpcService>(), regionService, new GameObjectBuilder(services, scriptProvider), Substitute.For<IGroundItemBuilder>(),
            Substitute.For<IMapper>(), new EntityStore());
        regionService.When(service => service.FlagCollision(Arg.Any<ILocation>(), Arg.Any<CollisionFlag>()))
            .Do(call =>
            {
                var location = call.ArgAt<ILocation>(0)!;
                if (location.RegionId == destination.Id)
                    destination.FlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, call.Arg<CollisionFlag>());
            });
        destination.MakeDynamic();
        var templates = new Dictionary<int, MapRegionTemplate> { [template.RegionId] = template };

        foreach (var (z, sourceZ) in mappings)
        {
            destination.WriteBlockByIndex(localPartX, localPartY, z, 50 * 8 + chunkX, 50 * 8 + chunkY, sourceZ, templates);
            destination.RotateBlockByIndex(localPartX, localPartY, z, rotation, templates);
        }

        var (expectedObjects, expectedFloor, shiftedObjects) = ClientDynamicChunkReference(template, chunkX, chunkY, mappings, rotation, localPartX * 8, localPartY * 8);
        Assert.IsTrue(shiftedObjects > 0, "The chunk must place objects on bridge tiles.");
        var actualObjects = destination.FindAllGameObjects()
            .Select(obj => $"{obj.Id}:{obj.Location.RegionLocalX},{obj.Location.RegionLocalY},{obj.Location.Z}:{obj.Rotation}")
            .ToArray();
        CollectionAssert.AreEquivalent(expectedObjects, actualObjects);
        var actualFloor = (from z in Enumerable.Range(0, 4)
            from x in Enumerable.Range(0, 64)
            from y in Enumerable.Range(0, 64)
            where destination.GetCollision(x, y, z).HasFlag(CollisionFlag.FloorBlock)
            select (x, y, z)).ToArray();
        CollectionAssert.AreEquivalent(expectedFloor, actualFloor);
    }

    /// <summary>
    /// The source chunk of region 12850 with the most plane-1 bridge tiles that also holds static
    /// objects on plane 1.
    /// </summary>
    private static (int ChunkX, int ChunkY) FindBridgeChunk12850(MapRegionTemplate template)
    {
        var chunks = from chunkX in Enumerable.Range(0, 8)
            from chunkY in Enumerable.Range(0, 8)
            let bridges = (from x in Enumerable.Range(chunkX * 8, 8)
                from y in Enumerable.Range(chunkY * 8, 8)
                where (template.GetTerrainFlags(1, x, y) & 0x2) != 0
                select (x, y)).Count()
            let objects = template.SourceObjects.Count(obj => obj.IsStatic && obj.Z == 1 && obj.LocalX >> 3 == chunkX && obj.LocalY >> 3 == chunkY)
            where bridges > 0 && objects > 0
            orderby bridges descending
            select (chunkX, chunkY);
        var chunk = chunks.FirstOrDefault((-1, -1));
        Assert.AreNotEqual((-1, -1), chunk, "Region 12850 fixture has no bridge chunk with plane-1 objects.");
        return chunk;
    }

    /// <summary>
    /// Reference of the client's dynamic chunk decoding (Map.readRegionPartLandscape and
    /// Class274.readGroundMapData): objects are selected by raw source plane, and objects and
    /// blocked tiles drop one plane where the destination tile's plane-1 terrain is a bridge.
    /// </summary>
    private static (string[] Objects, (int, int, int)[] Floor, int ShiftedObjects) ClientDynamicChunkReference(
        MapRegionTemplate template, int chunkX, int chunkY, (int Z, int SourceZ)[] mappings, int rotation, int destinationMinX, int destinationMinY)
    {
        var sourceMinX = chunkX * 8;
        var sourceMinY = chunkY * 8;
        (int X, int Y) Rotate(int x, int y) => rotation switch
        {
            0 => (x, y),
            1 => (y, 7 - x),
            2 => (7 - x, 7 - y),
            _ => (7 - y, x)
        };
        var plane1Source = mappings.Single(mapping => mapping.Z == 1).SourceZ;
        var bridges = new HashSet<(int, int)>();
        for (var x = 0; x < 8; x++)
        {
            for (var y = 0; y < 8; y++)
            {
                if ((template.GetTerrainFlags(plane1Source, sourceMinX + x, sourceMinY + y) & 0x2) != 0)
                {
                    var (dx, dy) = Rotate(x, y);
                    bridges.Add((destinationMinX + dx, destinationMinY + dy));
                }
            }
        }

        int Height(int z, int x, int y) => bridges.Contains((x, y)) ? z - 1 : z;
        var objects = new List<string>();
        var withoutCollision = new List<(string Key, int X, int Y, int Z, LayerType Layer)>();
        var occupied = new HashSet<(int, int, int, LayerType)>();
        var floor = new List<(int, int, int)>();
        var shifted = 0;
        foreach (var (z, sourceZ) in mappings)
        {
            foreach (var entry in template.SourceObjects.Where(obj => obj.Z == sourceZ
                         && obj.LocalX >= sourceMinX && obj.LocalX < sourceMinX + 8 && obj.LocalY >= sourceMinY && obj.LocalY < sourceMinY + 8))
            {
                var x = destinationMinX + MapRegion.CalculateObjectRotationDelta(entry.Definition.SizeX, entry.Definition.SizeY, entry.Rotation, entry.LocalX & 7, entry.LocalY & 7, rotation, false);
                var y = destinationMinY + MapRegion.CalculateObjectRotationDelta(entry.Definition.SizeX, entry.Definition.SizeY, entry.Rotation, entry.LocalX & 7, entry.LocalY & 7, rotation, true);
                var height = Height(z, x, y);
                if (x < 0 || y < 0 || x >= 64 || y >= 64)
                    continue;
                var key = $"{entry.Id}:{x},{y},{(height < 0 ? z : height)}:{(entry.Rotation + rotation) & 3}";
                if (height < 0)
                {
                    // Map.addToClip still builds the object when there is no collision plane. The server
                    // keeps it on its draw plane unless a reachable object holds that slot.
                    withoutCollision.Add((key, x, y, z, entry.Shape.GetLayerType()));
                    continue;
                }

                if (height != z)
                    shifted++;
                objects.Add(key);
                occupied.Add((x, y, height, entry.Shape.GetLayerType()));
            }

            for (var x = 0; x < 8; x++)
            {
                for (var y = 0; y < 8; y++)
                {
                    if ((template.GetTerrainFlags(sourceZ, sourceMinX + x, sourceMinY + y) & 0x1) == 0)
                        continue;
                    var (dx, dy) = Rotate(x, y);
                    var height = Height(z, destinationMinX + dx, destinationMinY + dy);
                    if (height >= 0)
                        floor.Add((destinationMinX + dx, destinationMinY + dy, height));
                }
            }
        }

        objects.AddRange(withoutCollision.Where(entry => !occupied.Contains((entry.X, entry.Y, entry.Z, entry.Layer))).Select(entry => entry.Key));
        return (objects.ToArray(), floor.Distinct().ToArray(), shifted);
    }

    private static string Fixture12850(string name) => Path.Join(AppContext.BaseDirectory, "Fixtures", "maps", name);

    private static int[][] Collision12850(IMapRegion region) => Enumerable.Range(0, 4).Select(plane => Enumerable.Range(0, 4096)
        .Select(tile => (int)region.GetCollision(tile / 64, tile % 64, plane)).ToArray()).ToArray();

    private static void AssertPinnedCollision12850(int[][] actual)
    {
        var expected = JsonSerializer.Deserialize<int[][]>(File.ReadAllText(Fixture12850("collision-12850.json")))!;
        Assert.AreEqual(expected.Length, actual.Length);
        for (var plane = 0; plane < actual.Length; plane++)
            CollectionAssert.AreEqual(expected[plane], actual[plane], $"Collision differs on plane {plane}.");
    }

    private static async Task<(MapRegion Region, MapRegionTemplate Template)> LoadRegion12850Async()
    {
        static string Fixture(string name) => Fixture12850(name);

        var cache = Substitute.For<ICacheAPI>();
        cache.GetFileId(5, "m50_50").Returns(1);
        cache.GetFileId(5, "l50_50").Returns(2);
        cache.ReadContainer(5, 1).Returns(_ => new Container(new MemoryStream(File.ReadAllBytes(Fixture("terrain-12850.dat")))));
        cache.ReadContainer(5, 2).Returns(_ => new Container(new MemoryStream(File.ReadAllBytes(Fixture("landscape-12850.dat")))));
        var mapProvider = new MapProvider(cache, new RegionMapArchive(cache), new MapCodec(), new MapTypeFactory(), NullLogger<MapProvider>.Instance);

        var definitions = JsonSerializer.Deserialize<CollisionDefinition[]>(
            File.ReadAllText(Fixture("object-collision-12850.json")), new JsonSerializerOptions(JsonSerializerDefaults.Web))!
            .ToDictionary(entry => entry.Id, entry =>
            {
                var definition = Substitute.For<IGameObjectDefinition>();
                definition.SizeX.Returns(entry.SizeX);
                definition.SizeY.Returns(entry.SizeY);
                definition.ClipType.Returns(entry.ClipType);
                definition.Solid.Returns(entry.Solid);
                definition.Gateway.Returns(entry.Gateway);
                return definition;
            });
        var objectService = Substitute.For<IGameObjectService>();
        objectService.FindGameObjectDefinitionsByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<int, IGameObjectDefinition>>(definitions));
        var scriptProvider = Substitute.For<IGameObjectScriptProvider>();
        scriptProvider.GetGameObjectScriptTypeById(Arg.Any<int>()).Returns(typeof(IGameObjectScript));
        var services = new ServiceCollection().AddSingleton(Substitute.For<IGameObjectScript>()).BuildServiceProvider();
        var builder = new GameObjectBuilder(services, scriptProvider);
        var store = new EntityStore();
        var regionService = Substitute.For<IMapRegionService>();
        var region = new MapRegion(Location.Create(3200, 3200, 0), [],
            Substitute.For<INpcService>(), regionService, builder, Substitute.For<IGroundItemBuilder>(),
            Substitute.For<IMapper>(), store);
        regionService.When(service => service.FlagCollision(Arg.Any<ILocation>(), Arg.Any<CollisionFlag>()))
            .Do(call =>
            {
                var location = call.ArgAt<ILocation>(0)!;
                if (location.RegionId == region.Id)
                    region.FlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, call.Arg<CollisionFlag>());
            });

        regionService.When(service => service.UnFlagCollision(Arg.Any<ILocation>(), Arg.Any<CollisionFlag>()))
            .Do(call =>
            {
                var location = call.ArgAt<ILocation>(0)!;
                if (location.RegionId == region.Id)
                    region.UnFlagCollision(location.RegionLocalX, location.RegionLocalY, location.Z, call.Arg<CollisionFlag>());
            });

        var fixture = CreateLoader(mapProvider: mapProvider, gameObjectBuilder: builder, entityStore: store,
            regionService: regionService, gameObjectService: objectService);
        await fixture.Loader.LoadAsync(region);
        var template = await new MapRegionTemplateLoader(EmptyObjectSpawnRepository(), objectService, mapProvider).LoadAsync(region.Id, []);
        return (region, template);
    }

    private static IGameObjectSpawnRepository EmptyObjectSpawnRepository()
    {
        var repository = Substitute.For<IGameObjectSpawnRepository>();
        repository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(_ => new TestAsyncEnumerable<GameobjectSpawn>([]));
        return repository;
    }

    private sealed record CollisionDefinition(int Id, int SizeX, int SizeY, int ClipType, bool Solid, bool Gateway);

    private static IMapProvider CreateMapProvider(DecodedMapRegion? decodedRegion = null)
    {
        var provider = Substitute.For<IMapProvider>();
        provider.DecodeRegion(Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<CancellationToken>())
            .Returns(decodedRegion ?? DecodedMapRegion.Empty);
        return provider;
    }

    private static IMapObject CreateMapObject(int id, int shapeType, int rotation, int x, int y, int z)
    {
        var mapObject = Substitute.For<IMapObject>();
        mapObject.Id.Returns(id);
        mapObject.ShapeType.Returns(shapeType);
        mapObject.Rotation.Returns(rotation);
        mapObject.X.Returns(x);
        mapObject.Y.Returns(y);
        mapObject.Z.Returns(z);
        return mapObject;
    }

    private static IMapRegion CreateRegion(Exception? loadFailure = null)
    {
        var state = MapRegionState.Initializing;
        var region = Substitute.For<IMapRegion>();
        region.Id.Returns(257);
        region.BaseLocation.Returns(Location.Create(64, 64, 0, 0));
        region.Size.Returns(Location.Create(64, 64, 4, 0));
        region.XteaKeys.Returns(new int[4]);
        region.State.Returns(_ => state);
        if (loadFailure is null)
        {
            region.When(value => value.MarkReady()).Do(_ => state = MapRegionState.Ready);
        }
        else
        {
            region.When(value => value.MarkReady()).Do(_ => throw loadFailure);
        }
        region.When(value => value.MarkDiscarded()).Do(_ => state = MapRegionState.Discarded);
        return region;
    }

    private static NpcSpawn[] CreateNpcSpawns(params int[] ids) => ids.Select(id => new NpcSpawn
    {
        NpcId = (ushort)id,
        CoordX = 64,
        CoordY = 64,
        CoordZ = 0,
        MinCoordX = 64,
        MinCoordY = 64,
        MinCoordZ = 0,
        MaxCoordX = 64,
        MaxCoordY = 64,
        MaxCoordZ = 0
    }).ToArray();

    private static INpc CreateNpc(int index)
    {
        var npc = Substitute.For<INpc>();
        npc.Index.Returns(index);
        return npc;
    }

    private static IGameObjectBuilder ConfigureStaticGameObjectBuilder(IGameObject gameObject)
    {
        var builder = Substitute.For<IGameObjectBuilder>();
        var id = Substitute.For<IGameObjectId>();
        var location = Substitute.For<IGameObjectLocation>();
        var optional = Substitute.For<IGameObjectOptional>();
        builder.Create().Returns(id);
        id.WithId(Arg.Any<int>()).Returns(location);
        location.WithLocation(Arg.Any<ILocation>()).Returns(optional);
        optional.WithDefinition(Arg.Any<IGameObjectDefinition>()).Returns(optional);
        optional.WithRotation(Arg.Any<int>()).Returns(optional);
        optional.WithShape(Arg.Any<ShapeType>()).Returns(optional);
        optional.WithDefinition(Arg.Any<IGameObjectDefinition>()).Returns(optional);
        optional.AsStatic().Returns(optional);
        optional.Build().Returns(gameObject);
        return builder;
    }

    private static IGroundItemBuilder ConfigureGroundItemBuilder(IGroundItem item)
    {
        var builder = Substitute.For<IGroundItemBuilder>();
        var onGround = Substitute.For<IGroundItemOnGround>();
        var location = Substitute.For<IGroundItemLocation>();
        var optional = Substitute.For<IGroundItemOptional>();
        builder.Create().Returns(onGround);
        onGround.WithItem(Arg.Any<Func<Hagalaz.Game.Abstractions.Builders.Item.IItemBuilder, Hagalaz.Game.Abstractions.Builders.Item.IItemBuild>>())
            .Returns(location);
        location.WithLocation(Arg.Any<ILocation>()).Returns(optional);
        optional.WithRespawnTicks(Arg.Any<int>()).Returns(optional);
        optional.Build().Returns(item);
        return builder;
    }

    private static GameObject CreateConcreteGameObject(ILocation location)
    {
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.ClipType.Returns(0);
        definition.Gateway.Returns(false);
        definition.Solid.Returns(false);
        definition.SizeX.Returns(1);
        definition.SizeY.Returns(1);
        return new GameObject(
            100,
            location,
            0,
            ShapeType.GroundDefault,
            true,
            definition,
            Substitute.For<IGameObjectScript>());
    }

    private static void AssertEntityResolves<TEntity>(IEntityStore store, IEntity<TEntity> expected)
        where TEntity : class, IEntity
    {
        Assert.AreNotEqual(default, expected.Handle);
        Assert.IsTrue(store.TryResolve(expected.Handle, out var actual));
        Assert.AreSame(expected, (IEntity<TEntity>?)actual);
    }

    private static IGameObjectBuilder ConfigureStaticGameObjectBuilderWithFailure(
        IGameObject firstObject,
        Exception secondBuildFailure)
    {
        var builder = Substitute.For<IGameObjectBuilder>();
        var firstId = Substitute.For<IGameObjectId>();
        var secondId = Substitute.For<IGameObjectId>();
        var location = Substitute.For<IGameObjectLocation>();
        var optional = Substitute.For<IGameObjectOptional>();
        builder.Create().Returns(firstId, secondId);
        firstId.WithId(Arg.Any<int>()).Returns(location);
        location.WithLocation(Arg.Any<ILocation>()).Returns(optional);
        optional.WithDefinition(Arg.Any<IGameObjectDefinition>()).Returns(optional);
        optional.WithRotation(Arg.Any<int>()).Returns(optional);
        optional.WithShape(Arg.Any<ShapeType>()).Returns(optional);
        optional.AsStatic().Returns(optional);
        optional.Build().Returns(firstObject);
        secondId.WithId(Arg.Any<int>()).Returns(_ => throw secondBuildFailure);
        return builder;
    }

    private static void ConfigureNpcBuilder(INpcBuilder builder, params (int Id, INpc Npc)[] entries)
    {
        var idBuilders = entries.Select(_ => Substitute.For<INpcId>()).ToArray();
        builder.Create().Returns(idBuilders[0], idBuilders.Skip(1).ToArray());
        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            var locationBuilder = Substitute.For<INpcLocation>();
            var optional = Substitute.For<INpcOptional>();
            idBuilders[index].WithId(entry.Id).Returns(locationBuilder);
            locationBuilder.WithLocation(Arg.Any<ILocation>()).Returns(optional);
            optional.WithMinimumBounds(Arg.Any<ILocation>()).Returns(optional);
            optional.WithMaximumBounds(Arg.Any<ILocation>()).Returns(optional);
            optional.WithFaceDirection(Arg.Any<DirectionFlag>()).Returns(optional);
            optional.Build().Returns(entry.Npc);
        }
    }

    private static LoaderFixture CreateLoader(
        IMapProvider? mapProvider = null,
        IEnumerable<NpcSpawn>? npcSpawns = null,
        IEnumerable<GameobjectSpawn>? objectSpawns = null,
        Exception? itemSourceFailure = null,
        IGameObjectBuilder? gameObjectBuilder = null,
        IGroundItemBuilder? groundItemBuilder = null,
        IEnumerable<ItemSpawn>? itemSpawns = null,
        IEntityStore? entityStore = null,
        IMapRegionService? regionService = null,
        IGameObjectService? gameObjectService = null)
    {
        var mapperConfiguration = new MapperConfiguration(
            configuration =>
            {
                configuration.AddProfile<NpcProfile>();
                configuration.AddProfile<ItemProfile>();
            },
            LoggerFactory.Create(_ => { }));

        var npcRepository = Substitute.For<INpcSpawnRepository>();
        npcRepository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new TestAsyncEnumerable<NpcSpawn>(npcSpawns ?? []));
        var itemRepository = Substitute.For<IGroundItemSpawnRepository>();
        if (itemSourceFailure is null)
        {
            itemRepository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(new TestAsyncEnumerable<ItemSpawn>(itemSpawns ?? []));
        }
        else
        {
            itemRepository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(_ => throw itemSourceFailure);
        }

        var objectRepository = Substitute.For<IGameObjectSpawnRepository>();
        objectRepository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new TestAsyncEnumerable<GameobjectSpawn>(objectSpawns ?? []));
        regionService ??= Substitute.For<IMapRegionService>();
        regionService.IsCurrentMapRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IMapRegion>()).Returns(true);
        if (gameObjectService is null)
        {
            gameObjectService = Substitute.For<IGameObjectService>();
            gameObjectService.FindGameObjectDefinitionsByIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    var definitions = callInfo.Arg<IEnumerable<int>>()
                        .Distinct()
                        .ToDictionary(id => id, _ => (IGameObjectDefinition)Substitute.For<IGameObjectDefinition>());
                    return Task.FromResult<IReadOnlyDictionary<int, IGameObjectDefinition>>(definitions);
                });
        }
        var npcService = Substitute.For<INpcService>();
        var npcBuilder = Substitute.For<INpcBuilder>();
        var loader = new MapRegionLoader(
            npcService,
            regionService,
            npcRepository,
            itemRepository,
            new MapRegionTemplateLoader(objectRepository, gameObjectService, mapProvider ?? CreateMapProvider()),
            new LocationBuilder(),
            groundItemBuilder ?? Substitute.For<IGroundItemBuilder>(),
            gameObjectBuilder ?? Substitute.For<IGameObjectBuilder>(),
            npcBuilder,
            mapperConfiguration.CreateMapper(),
            Substitute.For<ILogger<MapRegionLoader>>(),
            entityStore ?? new EntityStore());
        return new LoaderFixture(loader, regionService, npcService, npcBuilder, gameObjectService);
    }

    private sealed record LoaderFixture(
        MapRegionLoader Loader,
        IMapRegionService RegionService,
        INpcService NpcService,
        INpcBuilder NpcBuilder,
        IGameObjectService GameObjectService);

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable)
        {
        }

        public TestAsyncEnumerable(Expression expression) : base(expression)
        {
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new TestAsyncEnumerator<T>(((IEnumerable<T>)this).GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    private sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;

        public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());

        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments()[0];
            var queryType = typeof(TestAsyncEnumerable<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(queryType, expression)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);

        public object? Execute(Expression expression) => inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var resultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = inner.Execute(expression);
            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [executionResult])!;
        }
    }
}
