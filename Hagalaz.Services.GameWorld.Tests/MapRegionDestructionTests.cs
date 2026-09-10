using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Microsoft.Extensions.DependencyInjection;
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
        npcService.UnregisterAsync(first).Returns(Task.FromException(new InvalidOperationException("npc-a")));
        var region = CreateRegion(npcService);
        region.Add(first);
        region.Add(second);
        region.Add(third);

        await Assert.ThrowsExactlyAsync<AggregateException>(() => region.DestroyAsync());

        await npcService.Received(1).UnregisterAsync(first);
        await npcService.Received(1).UnregisterAsync(second);
        await npcService.Received(1).UnregisterAsync(third);
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
        var gameObject = CreateGameObject();
        region.Add(gameObject);

        await Assert.ThrowsExactlyAsync<AggregateException>(() => region.DestroyAsync());

        item.Received(1).Destroy();
        gameObject.Received(1).Destroy();
    }

    [TestMethod]
    public async Task DestroyAsync_AggregatesNpcItemAndObjectFailuresAndMarksRegionDestroyed()
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
        var gameObject = CreateGameObject();
        gameObject.When(objectToDestroy => objectToDestroy.Destroy())
            .Do(_ => throw new InvalidOperationException("object-failure"));
        region.Add(gameObject);

        var exception = await Assert.ThrowsExactlyAsync<AggregateException>(() => region.DestroyAsync());

        Assert.IsTrue(region.IsDestroyed);
        var messages = exception.Flatten().InnerExceptions.Select(error => error.Message).ToArray();
        CollectionAssert.AreEquivalent(
            new[] { "npc-failure", "item-failure", "object-failure" },
            messages);
    }

    [TestMethod]
    public async Task DestroyAsync_AfterFailureDoesNotRepeatCleanup()
    {
        var npcService = Substitute.For<INpcService>();
        var npc = CreateNpc(1);
        npcService.UnregisterAsync(npc).Returns(Task.FromException(new InvalidOperationException("npc")));
        var region = CreateRegion(npcService);
        region.Add(npc);

        await Assert.ThrowsExactlyAsync<AggregateException>(() => region.DestroyAsync());
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => region.DestroyAsync());

        await npcService.Received(1).UnregisterAsync(npc);
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

    private static IGameObject CreateGameObject()
    {
        var gameObject = Substitute.For<IGameObject>();
        gameObject.Location.Returns(Location.Create(2, 2, 0, 0));
        gameObject.ShapeType.Returns(ShapeType.GroundDefault);
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.ClipType.Returns(1);
        definition.SizeX.Returns(1);
        definition.SizeY.Returns(1);
        gameObject.Definition.Returns(definition);
        return gameObject;
    }
}
