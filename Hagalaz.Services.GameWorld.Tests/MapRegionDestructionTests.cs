using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.GameWorld.Model.Items;
using Hagalaz.Services.GameWorld.Model.Maps.GameObjects;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Hagalaz.Services.GameWorld.Store;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MapRegionDestructionTests
{
    [TestMethod]
    public void Destroy_WhenOneGameObjectFails_StillAttemptsLaterObjects()
    {
        var firstScript = Substitute.For<IGameObjectScript>();
        var secondScript = Substitute.For<IGameObjectScript>();
        var first = CreateGameObject(Location.Create(1, 1, 0, 0), firstScript);
        var second = CreateGameObject(Location.Create(2, 2, 0, 0), secondScript);
        var failure = new InvalidOperationException("object-a");
        firstScript.When(value => value.OnDestroy()).Do(_ => throw failure);
        var region = CreateRegion();
        region.Add(first);
        region.Add(second);

        var actual = Assert.ThrowsExactly<AggregateException>(() => region.Destroy());

        Assert.AreEqual(1, actual.InnerExceptions.Count);
        Assert.AreSame(failure, actual.InnerExceptions[0]);
        firstScript.Received(1).OnDestroy();
        secondScript.Received(1).OnDestroy();
    }

    [TestMethod]
    public void Destroy_CleansGroundItemsAndGameObjects()
    {
        var item = CreateGroundItem(Location.Create(1, 1, 0, 0));
        var gameObject = CreateGameObject(Location.Create(2, 2, 0, 0));
        var region = CreateRegion();
        region.Add(item);
        region.Add(gameObject);

        region.Destroy();

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
        var firstFailure = new InvalidOperationException("npc-a");
        var secondFailure = new InvalidOperationException("npc-b");
        var objectFailure = new InvalidOperationException("object-a");
        var npcService = Substitute.For<INpcService>();
        npcService.When(value => value.Unregister(first)).Do(_ => throw firstFailure);
        npcService.When(value => value.Unregister(second)).Do(_ => throw secondFailure);
        var item = CreateGroundItem(Location.Create(1, 1, 0, 0));
        var objectScript = Substitute.For<IGameObjectScript>();
        var gameObject = CreateGameObject(Location.Create(2, 2, 0, 0), objectScript);
        objectScript.When(value => value.OnDestroy()).Do(_ => throw objectFailure);
        var region = CreateRegion(npcService);
        region.Add(first);
        region.Add(second);
        region.Add(item);
        region.Add(gameObject);

        var actual = Assert.ThrowsExactly<AggregateException>(() => region.Destroy());

        CollectionAssert.AreEquivalent(
            new[] { firstFailure, secondFailure, objectFailure },
            actual.InnerExceptions);
        npcService.Received(1).Unregister(first);
        npcService.Received(1).Unregister(second);
        objectScript.Received(1).OnDestroy();
    }

    private static MapRegion CreateRegion(INpcService? npcService = null) => new(
        Location.Create(0, 0, 0, 0),
        [0, 0, 0, 0],
        npcService ?? Substitute.For<INpcService>(),
        Substitute.For<IMapRegionService>(),
        Substitute.For<IGameObjectBuilder>(),
        Substitute.For<IGroundItemBuilder>(),
        Substitute.For<AutoMapper.IMapper>(),
        new EntityStore());

    private static INpc CreateNpc(int index)
    {
        var npc = Substitute.For<INpc>();
        npc.Index.Returns(index);
        return npc;
    }

    private static GameObject CreateGameObject(ILocation location, IGameObjectScript? script = null)
    {
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.ClipType.Returns(0);
        definition.SizeX.Returns(1);
        definition.SizeY.Returns(1);
        return new GameObject(1, location, 0, ShapeType.GroundDefault, false, definition, script ?? Substitute.For<IGameObjectScript>());
    }

    private static GroundItem CreateGroundItem(ILocation location)
    {
        var item = Substitute.For<IItem>();
        return new GroundItem(
            item,
            location,
            null,
            0,
            0,
            Substitute.For<IMapRegionService>());
    }
}
