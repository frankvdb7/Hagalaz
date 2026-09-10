using System.Collections;
using System.IO;
using System.Linq.Expressions;
using AutoMapper;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Profiles;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MapRegionLoaderTests
{
    [TestMethod]
    public async Task LoadAsync_StagesStaticDecodeBeforeApplyingRegionState()
    {
        var decodeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseDecode = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var region = CreateRegion();
        var mapProvider = Substitute.For<IMapProvider>();
        mapProvider.When(provider => provider.DecodeRegion(
                Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<ObjectDecoded>(), Arg.Any<ImpassibleTerrainDecoded>()))
            .Do(callInfo =>
            {
                decodeStarted.TrySetResult();
                releaseDecode.Task.GetAwaiter().GetResult();
                callInfo.Arg<ImpassibleTerrainDecoded>()(1, 1, 0);
            });
        var fixture = CreateLoader(mapProvider: mapProvider);

        var loadTask = Task.Run(() => fixture.Loader.LoadAsync(region));
        await decodeStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.AreEqual(MapRegionState.Initializing, region.State);
        region.DidNotReceive().FlagCollision(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CollisionFlag>());

        releaseDecode.TrySetResult();
        await loadTask;

        Assert.AreEqual(MapRegionState.Ready, region.State);
        region.Received(1).FlagCollision(1, 1, 0, CollisionFlag.FloorBlock);
        region.Received(1).MarkReady();
    }

    [TestMethod]
    public async Task LoadAsync_WhenStaticDecodeFails_DoesNotApplyStagedDataOrRegisterNpcs()
    {
        var region = CreateRegion();
        var gameObject = Substitute.For<IGameObject>();
        var gameObjectBuilder = ConfigureStaticGameObjectBuilder(gameObject);
        var mapProvider = Substitute.For<IMapProvider>();
        var failure = new InvalidDataException("corrupt map data");
        mapProvider.When(provider => provider.DecodeRegion(
                Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<ObjectDecoded>(), Arg.Any<ImpassibleTerrainDecoded>()))
            .Do(callInfo =>
            {
                callInfo.Arg<ImpassibleTerrainDecoded>()(2, 2, 0);
                callInfo.Arg<ObjectDecoded>()(100, 0, 0, 3, 3, 0);
                throw failure;
            });
        var fixture = CreateLoader(mapProvider: mapProvider, gameObjectBuilder: gameObjectBuilder);

        var actual = await Assert.ThrowsExactlyAsync<InvalidDataException>(() => fixture.Loader.LoadAsync(region));

        Assert.AreSame(failure, actual);
        Assert.AreEqual(MapRegionState.Discarded, region.State);
        region.DidNotReceive().FlagCollision(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CollisionFlag>());
        region.DidNotReceive().Add(gameObject);
        await fixture.NpcService.DidNotReceive().RegisterAsync(Arg.Any<INpc>());
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
        await fixture.NpcService.Received(1).UnregisterAsync(npcA);
        await fixture.NpcService.DidNotReceive().UnregisterAsync(npcB);
        fixture.RegionService.Received(1).TryRemoveMapRegion(region.Id, region.BaseLocation.Dimension, region);
    }

    [TestMethod]
    public async Task LoadAsync_WhenCleanupOfRegisteredNpcsFails_ContinuesCleanupAndPreservesFailure()
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

        var actual = await Assert.ThrowsExactlyAsync<AggregateException>(() => fixture.Loader.LoadAsync(region));

        Assert.IsTrue(actual.InnerExceptions.Any(exception => ReferenceEquals(exception, fatalFailure)));
        Assert.IsTrue(actual.InnerExceptions.Any(exception => ReferenceEquals(exception, cleanupFailure)));
        await fixture.NpcService.Received(1).UnregisterAsync(npcA);
        await fixture.NpcService.Received(1).UnregisterAsync(npcB);
        fixture.RegionService.Received(1).TryRemoveMapRegion(region.Id, region.BaseLocation.Dimension, region);
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
        npc.IsDestroyed.Returns(false);
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
        optional.WithRotation(Arg.Any<int>()).Returns(optional);
        optional.WithShape(Arg.Any<ShapeType>()).Returns(optional);
        optional.AsStatic().Returns(optional);
        optional.Build().Returns(gameObject);
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
        Exception? itemSourceFailure = null,
        IGameObjectBuilder? gameObjectBuilder = null)
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
                .Returns(new TestAsyncEnumerable<ItemSpawn>([]));
        }
        else
        {
            itemRepository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(_ => throw itemSourceFailure);
        }

        var objectRepository = Substitute.For<IGameObjectSpawnRepository>();
        objectRepository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new TestAsyncEnumerable<GameobjectSpawn>([]));
        var regionService = Substitute.For<IMapRegionService>();
        regionService.IsCurrentMapRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IMapRegion>()).Returns(true);
        var npcService = Substitute.For<INpcService>();
        var npcBuilder = Substitute.For<INpcBuilder>();
        var loader = new MapRegionLoader(
            npcService,
            regionService,
            npcRepository,
            itemRepository,
            objectRepository,
            mapProvider ?? Substitute.For<IMapProvider>(),
            new LocationBuilder(),
            Substitute.For<IGroundItemBuilder>(),
            gameObjectBuilder ?? Substitute.For<IGameObjectBuilder>(),
            npcBuilder,
            mapperConfiguration.CreateMapper(),
            Substitute.For<ILogger<MapRegionLoader>>());
        return new LoaderFixture(loader, regionService, npcService, npcBuilder);
    }

    private sealed record LoaderFixture(
        MapRegionLoader Loader,
        IMapRegionService RegionService,
        INpcService NpcService,
        INpcBuilder NpcBuilder);

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
