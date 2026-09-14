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
    public void Destroy_WhenOneGameObjectFails_StillAttemptsLaterObjects()
    {
        var first = CreateGameObject(Location.Create(1, 1, 0, 0));
        var second = CreateGameObject(Location.Create(2, 2, 0, 0));
        var failure = new InvalidOperationException("object-a");
        first.When(value => value.Destroy()).Do(_ => throw failure);
        var region = CreateRegion();
        region.Add(first);
        region.Add(second);

        var actual = Assert.ThrowsExactly<InvalidOperationException>(() => region.Destroy());

        Assert.AreSame(failure, actual);
        first.Received(1).Destroy();
        second.Received(1).Destroy();
    }

    [TestMethod]
    public void Destroy_CleansGroundItemsAndGameObjects()
    {
        var item = Substitute.For<IGroundItem>();
        item.Location.Returns(Location.Create(1, 1, 0, 0));
        var gameObject = CreateGameObject(Location.Create(2, 2, 0, 0));
        var region = CreateRegion();
        region.Add(item);
        region.Add(gameObject);

        region.Destroy();

        item.Received(1).Destroy();
        gameObject.Received(1).Destroy();
    }

    [TestMethod]
    public void Destroy_UnregistersOwnedNpcsThroughTheRegionNpcService()
    {
        var npc = Substitute.For<INpc>();
        npc.Index.Returns(1);
        var npcService = Substitute.For<INpcService>();
        var region = CreateRegion(npcService);
        region.Add(npc);

        region.Destroy();

        npcService.Received(1).Unregister(npc);
    }

    [TestMethod]
    public void Destroy_WhenNpcUnregisterFails_StillAttemptsLaterNpcsAndResources()
    {
        var first = CreateNpc(1);
        var second = CreateNpc(2);
        var failure = new InvalidOperationException("npc-a");
        var npcService = Substitute.For<INpcService>();
        npcService.When(value => value.Unregister(first)).Do(_ => throw failure);
        var item = Substitute.For<IGroundItem>();
        item.Location.Returns(Location.Create(1, 1, 0, 0));
        var gameObject = CreateGameObject(Location.Create(2, 2, 0, 0));
        var region = CreateRegion(npcService);
        region.Add(first);
        region.Add(second);
        region.Add(item);
        region.Add(gameObject);

        var actual = Assert.ThrowsExactly<InvalidOperationException>(() => region.Destroy());

        Assert.AreSame(failure, actual);
        npcService.Received(1).Unregister(first);
        npcService.Received(1).Unregister(second);
        item.Received(1).Destroy();
        gameObject.Received(1).Destroy();
    }

    private static MapRegion CreateRegion(INpcService? npcService = null) => new(
        Location.Create(0, 0, 0, 0),
        [0, 0, 0, 0],
        npcService ?? Substitute.For<INpcService>(),
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
