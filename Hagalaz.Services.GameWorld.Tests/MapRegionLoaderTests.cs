using System.Collections;
using System.Linq.Expressions;
using AutoMapper;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Data.Entities;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Hagalaz.Services.GameWorld.Profiles;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MapRegionLoaderTests
{
    [TestMethod]
    public async Task LoadAsync_PublishesReadinessOnlyAfterStaticCollisionPopulationCompletes()
    {
        var staticPopulationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseStaticPopulation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loaded = false;
        var region = Substitute.For<IMapRegion, IMapRegionLoadRollback>();
        region.Id.Returns(257);
        region.BaseLocation.Returns(Location.Create(64, 64, 0, 0));
        region.Size.Returns(Location.Create(64, 64, 4, 0));
        region.XteaKeys.Returns(new int[4]);
        region.IsLoaded.Returns(_ => loaded);
        region.When(map => map.FlagCollision(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), CollisionFlag.FloorBlock))
            .Do(_ =>
            {
                staticPopulationStarted.TrySetResult();
                releaseStaticPopulation.Task.GetAwaiter().GetResult();
            });
        region.When(map => map.Load()).Do(_ => loaded = true);

        var mapProvider = Substitute.For<IMapProvider>();
        mapProvider.When(provider => provider.DecodeRegion(
                Arg.Any<int>(),
                Arg.Any<int[]>(),
                Arg.Any<ObjectDecoded>(),
                Arg.Any<ImpassibleTerrainDecoded>()))
            .Do(callInfo => callInfo.Arg<ImpassibleTerrainDecoded>()!(1, 1, 0));

        var loader = CreateLoader(mapProvider);
        var loadTask = Task.Run(() => loader.LoadAsync(region));

        await staticPopulationStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsFalse(region.IsLoaded);

        releaseStaticPopulation.TrySetResult();
        await loadTask;

        Assert.IsTrue(region.IsLoaded);
        region.Received(1).Load();
    }

    [TestMethod]
    public async Task LoadAsync_WhenPopulationFails_RollsBackAndRethrowsOriginalFailure()
    {
        var region = Substitute.For<IMapRegion, IMapRegionLoadRollback>();
        var rollback = (IMapRegionLoadRollback)region;
        region.Id.Returns(257);
        region.BaseLocation.Returns(Location.Create(64, 64, 0, 0));
        region.Size.Returns(Location.Create(64, 64, 4, 0));
        region.XteaKeys.Returns(new int[4]);
        region.IsLoaded.Returns(false);

        var mapProvider = Substitute.For<IMapProvider>();
        mapProvider.When(provider => provider.DecodeRegion(
                Arg.Any<int>(),
                Arg.Any<int[]>(),
                Arg.Any<ObjectDecoded>(),
                Arg.Any<ImpassibleTerrainDecoded>()))
            .Do(_ => throw new InvalidOperationException("test failure"));

        var loader = CreateLoader(mapProvider);
        var failure = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => loader.LoadAsync(region));

        Assert.AreEqual("test failure", failure.Message);
        Assert.IsFalse(region.IsLoaded);
        region.DidNotReceive().Load();
        await rollback.Received(1).ResetUnpublishedLoadAsync();
    }

    [TestMethod]
    public async Task LoadAsync_WhenCanceledDuringPopulation_RollsBackAndRethrowsCancellation()
    {
        var region = Substitute.For<IMapRegion, IMapRegionLoadRollback>();
        var rollback = (IMapRegionLoadRollback)region;
        region.Id.Returns(257);
        region.BaseLocation.Returns(Location.Create(64, 64, 0, 0));
        region.Size.Returns(Location.Create(64, 64, 4, 0));
        region.XteaKeys.Returns(new int[4]);
        region.IsLoaded.Returns(false);
        using var cancellation = new CancellationTokenSource();
        var mapProvider = Substitute.For<IMapProvider>();
        mapProvider.When(provider => provider.DecodeRegion(
                Arg.Any<int>(),
                Arg.Any<int[]>(),
                Arg.Any<ObjectDecoded>(),
                Arg.Any<ImpassibleTerrainDecoded>()))
            .Do(_ => cancellation.Cancel());

        var loader = CreateLoader(mapProvider);
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => loader.LoadAsync(region, cancellation.Token));

        Assert.IsFalse(region.IsLoaded);
        region.DidNotReceive().Load();
        await rollback.Received(1).ResetUnpublishedLoadAsync();
    }

    [TestMethod]
    public async Task LoadAsync_WhenRollbackFails_PreservesBothFailures()
    {
        var region = Substitute.For<IMapRegion, IMapRegionLoadRollback>();
        var rollback = (IMapRegionLoadRollback)region;
        region.Id.Returns(257);
        region.BaseLocation.Returns(Location.Create(64, 64, 0, 0));
        region.Size.Returns(Location.Create(64, 64, 4, 0));
        region.XteaKeys.Returns(new int[4]);
        region.IsLoaded.Returns(false);
        var rollbackFailure = new ApplicationException("rollback failure");
        rollback.ResetUnpublishedLoadAsync().Returns(Task.FromException(rollbackFailure));

        var mapProvider = Substitute.For<IMapProvider>();
        mapProvider.When(provider => provider.DecodeRegion(
                Arg.Any<int>(),
                Arg.Any<int[]>(),
                Arg.Any<ObjectDecoded>(),
                Arg.Any<ImpassibleTerrainDecoded>()))
            .Do(_ => throw new InvalidOperationException("load failure"));

        var loader = CreateLoader(mapProvider);
        var failure = await Assert.ThrowsExactlyAsync<AggregateException>(() => loader.LoadAsync(region));

        Assert.AreEqual(2, failure.InnerExceptions.Count);
        Assert.AreEqual("load failure", failure.InnerExceptions[0].Message);
        Assert.AreSame(rollbackFailure, failure.InnerExceptions[1]);
    }

    [DataTestMethod]
    [DataRow("npc")]
    [DataRow("item")]
    [DataRow("static")]
    [DataRow("non-static")]
    public async Task LoadAsync_WhenAnyPopulationStageFails_RollsBackTheUnpublishedAttempt(string stage)
    {
        var region = Substitute.For<IMapRegion, IMapRegionLoadRollback>();
        var rollback = (IMapRegionLoadRollback)region;
        region.Id.Returns(257);
        region.BaseLocation.Returns(Location.Create(64, 64, 0, 0));
        region.Size.Returns(Location.Create(64, 64, 4, 0));
        region.XteaKeys.Returns(new int[4]);
        region.IsLoaded.Returns(false);

        var mapProvider = Substitute.For<IMapProvider>();
        if (stage == "static")
        {
            mapProvider.When(provider => provider.DecodeRegion(
                    Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<ObjectDecoded>(), Arg.Any<ImpassibleTerrainDecoded>()))
                .Do(_ => throw new InvalidOperationException("static failure"));
        }

        var loader = CreateLoader(mapProvider, stage);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => loader.LoadAsync(region));

        region.DidNotReceive().Load();
        await rollback.Received(1).ResetUnpublishedLoadAsync();
    }

    [TestMethod]
    public async Task LoadAsync_CanRetryTheSameRegionInstanceAfterRollback()
    {
        var region = CreateRegion();
        var mapProvider = Substitute.For<IMapProvider>();
        var attempts = 0;
        mapProvider.When(provider => provider.DecodeRegion(
                Arg.Any<int>(), Arg.Any<int[]>(), Arg.Any<ObjectDecoded>(), Arg.Any<ImpassibleTerrainDecoded>()))
            .Do(_ =>
            {
                attempts++;
                if (attempts == 1)
                {
                    region.FlagCollision(1, 1, 0, CollisionFlag.FloorBlock);
                    throw new InvalidOperationException("first attempt failed");
                }
            });
        var loader = CreateLoader(mapProvider);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => loader.LoadAsync(region));

        Assert.IsFalse(region.IsLoaded);
        Assert.AreEqual(CollisionFlag.Walkable, region.GetCollision(1, 1, 0));
        await loader.LoadAsync(region);

        Assert.IsTrue(region.IsLoaded);
        Assert.AreEqual(2, attempts);
    }

    [TestMethod]
    public async Task ResetUnpublishedLoadAsync_IsSafeBeforeLoad()
    {
        var region = CreateRegion();

        await region.ResetUnpublishedLoadAsync();

        Assert.IsFalse(region.IsLoaded);
        Assert.IsFalse(region.IsDestroyed);
    }

    [TestMethod]
    public async Task ResetUnpublishedLoadAsync_RejectsLoadedRegion()
    {
        var region = CreateRegion();
        region.Load();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => region.ResetUnpublishedLoadAsync());
    }

    [TestMethod]
    public async Task ResetUnpublishedLoadAsync_RejectsDestroyedRegion()
    {
        var region = CreateRegion();
        await region.DestroyAsync();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => region.ResetUnpublishedLoadAsync());
    }

    [TestMethod]
    public async Task ResetUnpublishedLoadAsync_RejectsRegionContainingCharacters()
    {
        var region = CreateRegion();
        var character = Substitute.For<ICharacter>();
        character.Index.Returns(1);
        region.Add(character);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => region.ResetUnpublishedLoadAsync());
        Assert.IsTrue(region.FindAllCharacters().Contains(character));
    }

    [TestMethod]
    public async Task ResetUnpublishedLoadAsync_ContinuesCleanupAfterNpcFailureAndClearsCollision()
    {
        var npcService = Substitute.For<INpcService>();
        var firstNpc = Substitute.For<INpc>();
        firstNpc.Index.Returns(1);
        var secondNpc = Substitute.For<INpc>();
        secondNpc.Index.Returns(2);
        var firstFailure = new InvalidOperationException("first NPC cleanup failed");
        npcService.UnregisterAsync(firstNpc).Returns(Task.FromException(firstFailure));
        npcService.UnregisterAsync(secondNpc).Returns(Task.CompletedTask);
        var region = new MapRegion(
            Location.Create(64, 64, 0, 0),
            new int[4],
            npcService,
            Substitute.For<IMapRegionService>(),
            Substitute.For<IGameObjectBuilder>(),
            Substitute.For<IGroundItemBuilder>(),
            new MapperConfiguration(configuration => { }, LoggerFactory.Create(_ => { })).CreateMapper());
        region.Add(firstNpc);
        region.Add(secondNpc);
        region.FlagCollision(1, 1, 0, CollisionFlag.FloorBlock);

        var failure = await Assert.ThrowsExactlyAsync<AggregateException>(() => region.ResetUnpublishedLoadAsync());

        Assert.IsTrue(failure.InnerExceptions.Any(exception => ReferenceEquals(exception, firstFailure)));
        await npcService.Received(1).UnregisterAsync(firstNpc);
        await npcService.Received(1).UnregisterAsync(secondNpc);
        Assert.IsFalse(region.FindAllNpcs().Any());
        Assert.AreEqual(CollisionFlag.Walkable, region.GetCollision(1, 1, 0));
    }

    private static MapRegion CreateRegion() => new(
        Location.Create(64, 64, 0, 0),
        new int[4],
        Substitute.For<INpcService>(),
        Substitute.For<IMapRegionService>(),
        Substitute.For<IGameObjectBuilder>(),
        Substitute.For<IGroundItemBuilder>(),
        new MapperConfiguration(configuration => { }, LoggerFactory.Create(_ => { })).CreateMapper());

    private static MapRegionLoader CreateLoader(IMapProvider mapProvider, string? failureStage = null)
    {
        var emptyNpcs = new TestAsyncEnumerable<NpcSpawn>(Array.Empty<NpcSpawn>());
        var emptyItems = new TestAsyncEnumerable<ItemSpawn>(Array.Empty<ItemSpawn>());
        var emptyObjects = new TestAsyncEnumerable<GameobjectSpawn>(Array.Empty<GameobjectSpawn>());
        var mapperConfiguration = new MapperConfiguration(
            configuration =>
            {
                configuration.AddProfile<NpcProfile>();
                configuration.AddProfile<ItemProfile>();
            },
            LoggerFactory.Create(_ => { }));

        var npcRepository = Substitute.For<INpcSpawnRepository>();
        npcRepository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns(emptyNpcs);
        var itemRepository = Substitute.For<IGroundItemSpawnRepository>();
        itemRepository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns(emptyItems);
        var objectRepository = Substitute.For<IGameObjectSpawnRepository>();
        objectRepository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns(emptyObjects);

        var failure = new InvalidOperationException($"{failureStage} failure");
        if (failureStage == "npc")
        {
            npcRepository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(_ => throw failure);
        }
        else if (failureStage == "item")
        {
            itemRepository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(_ => throw failure);
        }
        else if (failureStage == "non-static")
        {
            objectRepository.FindByBounds(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(_ => throw failure);
        }

        return new MapRegionLoader(
            Substitute.For<INpcService>(),
            npcRepository,
            itemRepository,
            objectRepository,
            mapProvider,
            new LocationBuilder(),
            Substitute.For<IGroundItemBuilder>(),
            Substitute.For<IGameObjectBuilder>(),
            Substitute.For<INpcBuilder>(),
            mapperConfiguration.CreateMapper(),
            Substitute.For<ILogger<MapRegionLoader>>());
    }

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
