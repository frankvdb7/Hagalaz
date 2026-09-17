using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Model.Items;
using Hagalaz.Services.GameWorld.Model.Maps.GameObjects;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MapRegionEntityIdentityTests
{
    [TestMethod]
    public void AddGroundItem_RegistersAndRemoveInvalidatesItsHandle()
    {
        var fixture = CreateFixture();
        var item = CreateGroundItem(fixture.Service, Location.Create(64, 64, 0, 0));

        fixture.Service.AddGroundItem(item);

        AssertEntityResolves(fixture.Store, item);
        var handle = item.Handle;
        Assert.IsTrue(fixture.Service.RemoveGroundItem(item));
        Assert.IsFalse(fixture.Store.TryResolve(handle, out _));
    }

    [TestMethod]
    public void AddGameObject_RegistersAndPermanentRemoveInvalidatesItsHandle()
    {
        var fixture = CreateFixture();
        var gameObject = CreateGameObject(Location.Create(64, 65, 0, 0), isStatic: false);

        fixture.Service.AddGameObject(gameObject);

        AssertEntityResolves(fixture.Store, gameObject);
        var handle = gameObject.Handle;
        fixture.Service.RemoveGameObject(gameObject);
        Assert.IsFalse(fixture.Store.TryResolve(handle, out _));
    }

    [TestMethod]
    public void GroundItemSlotReuse_RejectsTheRemovedGeneration()
    {
        var fixture = CreateFixture();
        var first = CreateGroundItem(fixture.Service, Location.Create(64, 64, 0, 0));
        fixture.Service.AddGroundItem(first);
        var staleHandle = first.Handle;
        Assert.IsTrue(fixture.Service.RemoveGroundItem(first));

        var replacement = CreateGroundItem(fixture.Service, Location.Create(64, 64, 0, 0));
        fixture.Service.AddGroundItem(replacement);

        Assert.AreEqual(staleHandle.Slot, replacement.Handle.Slot);
        Assert.AreNotEqual(staleHandle.Generation, replacement.Handle.Generation);
        Assert.IsFalse(fixture.Store.TryResolve(staleHandle, out _));
        AssertEntityResolves(fixture.Store, replacement);
    }

    [TestMethod]
    public void StaticGameObject_DisableAndEnablePreservesItsHandle()
    {
        var fixture = CreateFixture();
        var gameObject = CreateGameObject(Location.Create(64, 65, 0, 0), isStatic: true);
        fixture.Service.AddGameObject(gameObject);
        var handle = gameObject.Handle;

        fixture.Service.RemoveGameObject(gameObject);
        Assert.IsTrue(gameObject.IsDisabled);
        AssertEntityResolves(fixture.Store, gameObject);

        fixture.Service.AddGameObject(gameObject);

        Assert.AreEqual(handle, gameObject.Handle);
        Assert.IsFalse(gameObject.IsDisabled);
        AssertEntityResolves(fixture.Store, gameObject);
    }

    [TestMethod]
    public void Destroy_UnregistersDisabledStaticGameObject()
    {
        var fixture = CreateFixture();
        var gameObject = CreateGameObject(Location.Create(64, 65, 0, 0), isStatic: true);
        fixture.Service.AddGameObject(gameObject);
        var handle = gameObject.Handle;
        var region = fixture.Service.FindMapRegion(gameObject.Location.RegionId, gameObject.Location.Dimension)!;

        fixture.Service.RemoveGameObject(gameObject);
        region.Destroy();

        Assert.IsFalse(fixture.Store.TryResolve(handle, out _));
    }

    private static void AssertEntityResolves(IEntityStore store, IEntity expected)
    {
        Assert.AreNotEqual(default, expected.Handle);
        Assert.IsTrue(store.TryResolve(expected.Handle, out var actual));
        Assert.AreSame(expected, actual);
    }

    private static (MapRegionService Service, EntityStore Store) CreateFixture()
    {
        var store = new EntityStore();
        var provider = new ServiceCollection()
            .AddSingleton(Substitute.For<INpcService>())
            .BuildServiceProvider();
        var service = new MapRegionService(
            provider,
            new LocationBuilder(),
            Substitute.For<IGameObjectBuilder>(),
            Substitute.For<IGroundItemBuilder>(),
            Substitute.For<ILogger<MapRegionService>>(),
            Substitute.For<IMapper>(),
            Substitute.For<IMapRegionLoadScheduler>(),
            Substitute.For<IRsTaskService>(),
            store);
        return (service, store);
    }

    private static GroundItem CreateGroundItem(IMapRegionService service, ILocation location) => new(
        Substitute.For<IItem>(),
        location,
        null,
        0,
        0,
        service);

    private static GameObject CreateGameObject(ILocation location, bool isStatic)
    {
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.ClipType.Returns(0);
        definition.Gateway.Returns(false);
        definition.Solid.Returns(false);
        definition.SizeX.Returns(1);
        definition.SizeY.Returns(1);
        return new GameObject(
            1,
            location,
            0,
            ShapeType.GroundDefault,
            isStatic,
            definition,
            Substitute.For<IGameObjectScript>());
    }
}
