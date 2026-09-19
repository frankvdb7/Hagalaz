using AutoMapper;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Location;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Data;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Model.Items;
using Hagalaz.Services.GameWorld.Model.Maps.GameObjects;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MapRegionStaleMutationTests
{
    [TestMethod]
    public void RemoveGroundItem_AfterRegionRemoval_DoesNotRecreateOrLoadRegion()
    {
        var loadScheduler = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = CreateProvider(loadScheduler);
        var service = CreateService(provider);
        var item = CreateGroundItem(service, Location.Create(64, 64, 0, 0));

        service.AddGroundItem(item);
        var originalRegion = service.FindMapRegion(item.Location.RegionId, item.Location.Dimension)!;
        Assert.IsTrue(service.TryRemoveMapRegion(originalRegion.Id, 0, originalRegion));

        Assert.IsFalse(service.RemoveGroundItem(item));
        Assert.IsNull(service.FindMapRegion(originalRegion.Id, 0));
        loadScheduler.Received(1).RequestLoad(originalRegion);
    }

    [TestMethod]
    public void RemoveGameObject_AfterRegionRemoval_DoesNotRecreateOrLoadRegion()
    {
        var loadScheduler = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = CreateProvider(loadScheduler);
        var service = CreateService(provider);
        var gameObject = CreateGameObject(Location.Create(64, 65, 0, 0));

        service.AddGameObject(gameObject);
        var originalRegion = service.FindMapRegion(gameObject.Location.RegionId, gameObject.Location.Dimension)!;
        Assert.IsTrue(service.TryRemoveMapRegion(originalRegion.Id, 0, originalRegion));

        service.RemoveGameObject(gameObject);

        Assert.IsNull(service.FindMapRegion(originalRegion.Id, 0));
        loadScheduler.Received(1).RequestLoad(originalRegion);
    }

    [TestMethod]
    public void RemoveGroundItem_FromSuspendedRegion_DoesNotResumeRegion()
    {
        var loadScheduler = Substitute.For<IMapRegionLoadScheduler>();
        using var provider = CreateProvider(loadScheduler);
        var service = CreateService(provider);
        var item = CreateGroundItem(service, Location.Create(64, 64, 0, 0));

        service.AddGroundItem(item);
        var region = service.FindMapRegion(item.Location.RegionId, item.Location.Dimension)!;
        region.MarkReady();
        Assert.IsTrue(service.TrySuspendMapRegion(region));

        Assert.IsTrue(service.RemoveGroundItem(item));

        Assert.AreSame(region, service.FindMapRegion(region.Id, 0));
        Assert.IsTrue(service.FindIdleRegionsByDimension(0).Contains(region));
        Assert.IsFalse(service.FindRegionsByDimension(0).Contains(region));
        Assert.IsFalse(region.FindAllGroundItems().Contains(item));
    }

    [TestMethod]
    public void DelayedGameObjectUpdate_AfterRegionReplacement_DoesNotMutateReplacementOrQueueStaleUpdate()
    {
        var loadScheduler = Substitute.For<IMapRegionLoadScheduler>();
        var mapper = Substitute.For<IMapper>();
        mapper.Map<RaidoMessage>(Arg.Any<object>()).Returns(Substitute.For<RaidoMessage>());
        using var provider = CreateProvider(loadScheduler);
        var service = CreateService(provider, mapper);
        var location = Location.Create(65, 65, 0, 0);
        var staleObject = CreateWallGameObject(location, 0);

        service.AddGameObject(staleObject);
        var originalRegion = service.FindMapRegion(location.RegionId, location.Dimension)!;
        service.RemoveGameObject(staleObject);
        Assert.IsTrue(service.TryRemoveMapRegion(originalRegion.Id, 0, originalRegion));

        var replacementObject = CreateWallGameObject(location, 0);
        service.AddGameObject(replacementObject);
        var replacementRegion = service.FindMapRegion(location.RegionId, location.Dimension)!;
        replacementRegion.MajorClientPrepareUpdateTick();
        replacementRegion.MajorClientUpdateResetTick();

        var character = Substitute.For<ICharacter>();
        character.Index.Returns(1);
        character.Viewport.Returns(Substitute.For<IViewport>());
        character.Viewport.InBounds(Arg.Any<ILocation>()).Returns(true);
        character.Session.Returns(Substitute.For<IGameSession>());
        replacementRegion.Add(character);

        var objectService = new GameObjectService(
            service,
            Substitute.For<IGameObjectDefinitionRepository>(),
            Substitute.For<ITypeProvider<GameObjectDefinition>>());

        objectService.UpdateGameObject(new GameObjectUpdate
        {
            Instance = staleObject,
            Id = staleObject.Id,
            Rotation = 1
        });

        Assert.AreEqual(CollisionFlag.WallWest | CollisionFlag.WallAllowRangeWest, replacementRegion.GetCollision(location.RegionLocalX, location.RegionLocalY, 0));
        Assert.AreEqual(CollisionFlag.WallEast | CollisionFlag.WallAllowRangeEast, replacementRegion.GetCollision(location.RegionLocalX - 1, location.RegionLocalY, 0));

        replacementRegion.MajorClientPrepareUpdateTick();
        replacementRegion.MajorClientUpdateTick(new Dictionary<int, ICharacter>());

        mapper.DidNotReceive().Map<RaidoMessage>(Arg.Any<object>());
    }

    private static ServiceProvider CreateProvider(IMapRegionLoadScheduler loadScheduler) => new ServiceCollection()
        .AddSingleton(Substitute.For<INpcService>())
        .AddSingleton<IEntityStore, EntityStore>()
        .AddSingleton(loadScheduler)
        .AddSingleton<IRsTaskService>(Substitute.For<IRsTaskService>())
        .BuildServiceProvider();

    private static MapRegionService CreateService(IServiceProvider provider, IMapper? mapper = null) => new(
        provider,
        new LocationBuilder(),
        Substitute.For<IGameObjectBuilder>(),
        Substitute.For<IGroundItemBuilder>(),
        Substitute.For<ILogger<MapRegionService>>(),
        mapper ?? Substitute.For<IMapper>(),
        provider.GetRequiredService<IMapRegionLoadScheduler>(),
        provider.GetRequiredService<IRsTaskService>(),
        provider.GetRequiredService<IEntityStore>());

    private static GroundItem CreateGroundItem(IMapRegionService service, ILocation location) => new(
        Substitute.For<IItem>(),
        location,
        null,
        0,
        0,
        service);

    private static GameObject CreateGameObject(ILocation location) => new(
        42,
        location,
        0,
        ShapeType.GroundDefault,
        false,
        CreateDefinition(),
        Substitute.For<IGameObjectScript>());

    private static GameObject CreateWallGameObject(ILocation location, int rotation)
    {
        var script = Substitute.For<IGameObjectScript>();
        script.CanRenderFor(Arg.Any<ICharacter>()).Returns(true);
        return new GameObject(
            42,
            location,
            rotation,
            ShapeType.Wall,
            false,
            CreateDefinition(clipType: 1),
            script);
    }

    private static IGameObjectDefinition CreateDefinition(int clipType = 0)
    {
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.ClipType.Returns(clipType);
        definition.Gateway.Returns(false);
        definition.SizeX.Returns(1);
        definition.SizeY.Returns(1);
        return definition;
    }
}
