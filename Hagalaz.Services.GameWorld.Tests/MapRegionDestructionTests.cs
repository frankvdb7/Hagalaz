using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MapRegionDestructionTests
{
    [TestMethod]
    public async Task DestroyAsync_WhenOneNpcFails_StillAttemptsLaterNpcs()
    {
        var npcService = Substitute.For<INpcService>();
        var first = CreateNpc(1);
        var second = CreateNpc(2);
        var third = CreateNpc(3);
        var region = CreateRegion(npcService);
        region.Add(first);
        region.Add(second);
        region.Add(third);
        npcService.UnregisterAsync(first)
            .Returns(_ =>
            {
                region.Remove(first);
                return Task.FromException(new InvalidOperationException("npc-a"));
            });

        await Assert.ThrowsExactlyAsync<AggregateException>(() => region.DestroyAsync());

        await npcService.Received(1).UnregisterAsync(first);
        await npcService.Received(1).UnregisterAsync(second);
        await npcService.Received(1).UnregisterAsync(third);
    }

    [TestMethod]
    public async Task DestroyAsync_WhenNpcCleanupRemovesLaterNpc_StillAttemptsTheSnapshotEntry()
    {
        var npcService = Substitute.For<INpcService>();
        var first = CreateNpc(1);
        var second = CreateNpc(2);
        var region = CreateRegion(npcService);
        region.Add(first);
        region.Add(second);
        npcService.UnregisterAsync(first).Returns(_ =>
        {
            region.Remove(second);
            return Task.CompletedTask;
        });

        await region.DestroyAsync();

        await npcService.Received(1).UnregisterAsync(first);
        await npcService.Received(1).UnregisterAsync(second);
    }

    [TestMethod]
    public async Task DestroyAsync_WhenNpcCleanupRemovesItemsAndObjects_StillAttemptsTheirSnapshots()
    {
        var npcService = Substitute.For<INpcService>();
        var npc = CreateNpc(1);
        var item = Substitute.For<IGroundItem>();
        item.Location.Returns(Location.Create(1, 1, 0, 0));
        var gameObject = CreateGameObject(Location.Create(2, 2, 0, 0));
        var itemDestroyed = false;
        var gameObjectDestroyed = false;
        item.IsDestroyed.Returns(_ => itemDestroyed);
        item.When(itemToDestroy => itemToDestroy.Destroy()).Do(_ => itemDestroyed = true);
        gameObject.IsDestroyed.Returns(_ => gameObjectDestroyed);
        gameObject.When(objectToDestroy => objectToDestroy.Destroy()).Do(_ => gameObjectDestroyed = true);
        var region = CreateRegion(npcService);
        region.Add(npc);
        region.Add(item);
        region.Add(gameObject);
        npcService.UnregisterAsync(npc).Returns(_ =>
        {
            region.Remove(item);
            region.Remove(gameObject);
            return Task.CompletedTask;
        });

        await region.DestroyAsync();

        await npcService.Received(1).UnregisterAsync(npc);
        item.Received(1).Destroy();
        gameObject.Received(1).Destroy();
        Assert.IsTrue(itemDestroyed);
        Assert.IsTrue(gameObjectDestroyed);
    }

    [TestMethod]
    public async Task DestroyAsync_WhenNpcFails_StillDestroysItemsAndObjects()
    {
        var npcService = Substitute.For<INpcService>();
        var npc = CreateNpc(1);
        npcService.UnregisterAsync(npc).Returns(Task.FromException(new InvalidOperationException("npc")));
        var region = CreateRegion(npcService);
        region.Add(npc);
        var item = Substitute.For<IGroundItem>();
        item.Location.Returns(Location.Create(1, 1, 0, 0));
        region.Add(item);
        var gameObject = CreateGameObject(Location.Create(2, 2, 0, 0));
        region.Add(gameObject);

        await Assert.ThrowsExactlyAsync<AggregateException>(() => region.DestroyAsync());

        item.Received(1).Destroy();
        gameObject.Received(1).Destroy();
    }

    [TestMethod]
    public async Task DestroyAsync_AggregatesFailuresAndKeepsRegionPending()
    {
        var npcService = Substitute.For<INpcService>();
        var npc = CreateNpc(1);
        npcService.UnregisterAsync(npc).Returns(Task.FromException(new InvalidOperationException("npc-failure")));
        var region = CreateRegion(npcService);
        region.Add(npc);
        var item = Substitute.For<IGroundItem>();
        item.Location.Returns(Location.Create(1, 1, 0, 0));
        item.When(itemToDestroy => itemToDestroy.Destroy())
            .Do(_ => throw new InvalidOperationException("item-failure"));
        region.Add(item);
        var gameObject = CreateGameObject(Location.Create(2, 2, 0, 0));
        gameObject.When(objectToDestroy => objectToDestroy.Destroy())
            .Do(_ => throw new InvalidOperationException("object-failure"));
        region.Add(gameObject);

        var exception = await Assert.ThrowsExactlyAsync<AggregateException>(() => region.DestroyAsync());

        Assert.IsFalse(region.IsDestroyed);
        Assert.AreEqual(MapRegionDestructionState.Destroying, region.DestructionState);
        var messages = exception.Flatten().InnerExceptions.Select(error => error.Message).ToArray();
        CollectionAssert.AreEquivalent(
            new[] { "npc-failure", "item-failure", "object-failure" },
            messages);
    }

    [TestMethod]
    public async Task DestroyAsync_AfterFailureCanRetryCleanup()
    {
        var npcService = Substitute.For<INpcService>();
        var npc = CreateNpc(1);
        var attempts = 0;
        npcService.UnregisterAsync(npc).Returns(_ =>
        {
            attempts++;
            return attempts == 1
                ? Task.FromException(new InvalidOperationException("npc"))
                : Task.CompletedTask;
        });
        var region = CreateRegion(npcService);
        region.Add(npc);

        await Assert.ThrowsExactlyAsync<AggregateException>(() => region.DestroyAsync());
        await region.DestroyAsync();

        await npcService.Received(2).UnregisterAsync(npc);
        Assert.IsTrue(region.IsDestroyed);
    }

    [TestMethod]
    public async Task DestroyAsync_ConcurrentCallersDoNotDuplicateCleanup()
    {
        var npcService = Substitute.For<INpcService>();
        var npc = CreateNpc(1);
        var region = CreateRegion(npcService);
        region.Add(npc);

        var first = region.DestroyAsync();
        var second = region.DestroyAsync();
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Task.WhenAll(first, second));

        await npcService.Received(1).UnregisterAsync(npc);
    }

    [TestMethod]
    public async Task DestroyAsync_RejectsNewNpcAfterDestructionStarts()
    {
        var npcService = Substitute.For<INpcService>();
        var existing = CreateNpc(1);
        var region = CreateRegion(npcService);
        region.Add(existing);
        npcService.UnregisterAsync(existing).Returns(Task.FromException(new InvalidOperationException("retry")));

        await Assert.ThrowsExactlyAsync<AggregateException>(() => region.DestroyAsync());

        Assert.ThrowsExactly<InvalidOperationException>(() => region.Add(CreateNpc(2)));
    }

    private static MapRegion CreateRegion(INpcService npcService) => new(
        Location.Create(0, 0, 0, 0),
        [0, 0, 0, 0],
        npcService,
        Substitute.For<IMapRegionService>(),
        Substitute.For<IGameObjectBuilder>(),
        Substitute.For<IGroundItemBuilder>(),
        Substitute.For<AutoMapper.IMapper>());

    private static INpc CreateNpc(int index)
    {
        var npc = Substitute.For<INpc>();
        npc.Index.Returns(index);
        return npc;
    }

    private static IGameObject CreateGameObject(ILocation location)
    {
        var gameObject = Substitute.For<IGameObject>();
        gameObject.Location.Returns(location);
        gameObject.ShapeType.Returns(ShapeType.GroundDefault);
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.ClipType.Returns(1);
        definition.SizeX.Returns(1);
        definition.SizeY.Returns(1);
        gameObject.Definition.Returns(definition);
        return gameObject;
    }
}
