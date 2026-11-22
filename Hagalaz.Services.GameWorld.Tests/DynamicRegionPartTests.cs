using AutoMapper;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Messages.Protocol.Map;
using Hagalaz.Services.GameWorld.Builders;
using Hagalaz.Services.GameWorld.Model.Maps.GameObjects;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class DynamicRegionPartTests
{
    // Source region (50, 51); source part local (2, 3) on plane 1 => absolute source part (402, 411).
    private const int SourceRegionX = 50;
    private const int SourceRegionY = 51;
    private const int SourceRegionId = (SourceRegionX << 8) | SourceRegionY;
    private const int SourcePartX = SourceRegionX * 8 + 2;
    private const int SourcePartY = SourceRegionY * 8 + 3;
    private const int SourceZ = 1;
    private const int SourceMinX = 16;
    private const int SourceMinY = 24;

    // Destination region (100, 90); destination local part (5, 6) on plane 0.
    private const int DestinationRegionX = 100;
    private const int DestinationRegionY = 90;
    private const int DestinationLocalPartX = 5;
    private const int DestinationLocalPartY = 6;
    private const int DestinationMinX = 40;
    private const int DestinationMinY = 48;

    private static readonly IGameObjectDefinition Standard1X1 = Definition(1, 1);
    private static readonly IGameObjectDefinition Standard2X3 = Definition(2, 3);
    private static readonly IGameObjectDefinition WallDefinition = Definition(1, 1);
    private static readonly IGameObjectDefinition DecorationDefinition = Definition(1, 1);

    // Chunk-local source objects: (id, x, y, rotation, shape, definition).
    private static readonly (int Id, int X, int Y, int Rotation, ShapeType Shape, IGameObjectDefinition Definition)[] SourceObjects =
    [
        (1, 1, 2, 0, ShapeType.GroundDefault, Standard1X1),
        (2, 4, 1, 0, ShapeType.GroundDefault, Standard2X3),
        (3, 0, 5, 0, ShapeType.Wall, WallDefinition),
        (4, 6, 6, 1, ShapeType.WallDecorationStraightXOffset, DecorationDefinition),
        (5, 3, 7, 0, ShapeType.GroundDecoration, DecorationDefinition),
    ];

    private static readonly (int X, int Y) SourceFloorBlock = (7, 0);

    // Expected chunk-local positions per part rotation, derived by hand from the client's
    // dynamic chunk decoding (1x1: r1 (y, 7-x), r2 (7-x, 7-y), r3 (7-y, x); the 2x3 object
    // subtracts its footprint).
    private static readonly Dictionary<int, (int X, int Y)[]> ExpectedObjectPositions = new()
    {
        [0] = [(1, 2), (4, 1), (0, 5), (6, 6), (3, 7)],
        [1] = [(2, 6), (1, 2), (5, 7), (6, 1), (7, 4)],
        [2] = [(6, 5), (2, 4), (7, 2), (1, 1), (4, 0)],
        [3] = [(5, 1), (4, 4), (2, 0), (1, 6), (0, 3)],
    };

    private static readonly Dictionary<int, (int X, int Y)> ExpectedFloorBlock = new()
    {
        [0] = (7, 0),
        [1] = (0, 0),
        [2] = (0, 7),
        [3] = (7, 7),
    };

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void WriteAndRotate_PlacesRotatedObjectsAtDestination(int rotation)
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, CreateSourceTemplate());
        destination.RotateBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, rotation, CreateSourceTemplate());

        var objects = destination.FindAllGameObjects().ToList();
        Assert.AreEqual(SourceObjects.Length, objects.Count);
        for (var index = 0; index < SourceObjects.Length; index++)
        {
            var source = SourceObjects[index];
            var (expectedX, expectedY) = ExpectedObjectPositions[rotation][index];
            var actual = objects.Single(obj => obj.Id == source.Id);
            Assert.AreEqual(DestinationRegionX * 64 + DestinationMinX + expectedX, actual.Location.X, $"X of object {source.Id}");
            Assert.AreEqual(DestinationRegionY * 64 + DestinationMinY + expectedY, actual.Location.Y, $"Y of object {source.Id}");
            Assert.AreEqual(0, actual.Location.Z);
            Assert.AreEqual((source.Rotation + rotation) & 0x3, actual.Rotation, $"rotation of object {source.Id}");
            Assert.AreEqual(source.Shape, actual.ShapeType);
            Assert.AreEqual(source.Shape.GetLayerType(), actual.ShapeType.GetLayerType());
            Assert.AreSame(source.Definition, actual.Definition);
            Assert.IsTrue(actual.IsStatic);
        }

        var stoodOn = destination.FindGameObjects(DestinationMinX + ExpectedObjectPositions[rotation][1].X,
            DestinationMinY + ExpectedObjectPositions[rotation][1].Y, 0).Single();
        Assert.AreEqual(rotation % 2 == 0 ? (2, 3) : (3, 2), FootprintOf(stoodOn));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void WriteAndRotate_CollisionEqualsStandardRegionWithRotatedContent(int rotation)
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        var expected = world.CreateRegion(DestinationRegionX, DestinationRegionY, dimension: 1);
        for (var index = 0; index < SourceObjects.Length; index++)
        {
            var source = SourceObjects[index];
            var (x, y) = ExpectedObjectPositions[rotation][index];
            expected.Add(TestWorld.CreateGameObject(source.Id, DestinationRegionX * 64 + DestinationMinX + x, DestinationRegionY * 64 + DestinationMinY + y, 0, 1,
                (source.Rotation + rotation) & 0x3, source.Shape, source.Definition));
        }

        var floor = ExpectedFloorBlock[rotation];
        expected.FlagCollision(DestinationMinX + floor.X, DestinationMinY + floor.Y, 0, CollisionFlag.FloorBlock);

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, CreateSourceTemplate());
        destination.RotateBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, rotation, CreateSourceTemplate());

        var expectedCollision = Collision(expected);
        Assert.IsTrue(expectedCollision.Any(flag => flag.HasFlag(CollisionFlag.WallAllowRangeWest)
                                                    || flag.HasFlag(CollisionFlag.WallAllowRangeNorth)
                                                    || flag.HasFlag(CollisionFlag.WallAllowRangeEast)
                                                    || flag.HasFlag(CollisionFlag.WallAllowRangeSouth)));
        Assert.IsTrue(expectedCollision.Any(flag => flag.HasFlag(CollisionFlag.ObjectBlock)));
        Assert.IsTrue(expectedCollision.Any(flag => flag.HasFlag(CollisionFlag.FloorDecorationBlock)));
        CollectionAssert.AreEqual(expectedCollision, Collision(destination));
    }

    [TestMethod]
    public void Write_ToDifferentLocalPart_KeepsSourceMappingAndReadsSourcePlaneOnly()
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        var distractors = new[]
        {
            // Same area, destination plane: must not be copied.
            new MapRegionTemplateObject(90, SourceMinX + 1, SourceMinY + 1, 0, 0, ShapeType.GroundDefault, Standard1X1, true),
            // Source plane, neighbouring source part: must not be copied.
            new MapRegionTemplateObject(91, SourceMinX + 8, SourceMinY, SourceZ, 0, ShapeType.GroundDefault, Standard1X1, true),
        };
        var template = CreateSourceTemplate(distractors);

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, template);

        var part = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0);
        Assert.IsTrue(part.HasDrawSource);
        Assert.AreEqual(SourcePartX, part.DrawRegionPartX);
        Assert.AreEqual(SourcePartY, part.DrawRegionPartY);
        Assert.AreEqual(SourceZ, part.DrawRegionZ);
        CollectionAssert.AreEquivalent(SourceObjects.Select(obj => obj.Id).ToArray(),
            destination.FindAllGameObjects().Select(obj => obj.Id).ToArray());
        Assert.IsTrue(destination.FindAllGameObjects().All(obj => obj.Location.Z == 0 && obj.Location.Dimension == 0));
    }

    [TestMethod]
    public void Write_FromAnotherDimensionUsesTemplateAndDestinationDimension()
    {
        var world = new TestWorld();
        var destination = world.CreateRegion(DestinationRegionX, DestinationRegionY, dimension: 1);
        destination.MakeDynamic();

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, CreateSourceTemplate());

        Assert.AreEqual(SourceObjects.Length, destination.FindAllGameObjects().Count());
        Assert.IsTrue(destination.FindAllGameObjects().All(obj => obj.Location.Dimension == 1));
        world.RegionService.DidNotReceiveWithAnyArgs().GetOrCreateMapRegion(default, default);
        world.RegionService.DidNotReceiveWithAnyArgs().FindMapRegion(default, default);
    }

    [TestMethod]
    public void Write_RejectsTemplateOfAnotherRegionWithoutChangingThePart()
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, CreateSourceTemplate());

        Assert.ThrowsExactly<ArgumentException>(() => destination.WriteBlockByIndex(
            DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX + 8, SourcePartY, SourceZ, CreateSourceTemplate()));

        Assert.AreEqual(SourcePartX, destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0).DrawRegionPartX);
        Assert.AreEqual(SourceObjects.Length, destination.FindAllGameObjects().Count());
    }

    [TestMethod]
    public void RotateAndDelete_UseTheLocalPartYConversion()
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, CreateSourceTemplate());
        destination.WriteBlockByIndex(DestinationLocalPartY, DestinationLocalPartX, 0, SourcePartX, SourcePartY, SourceZ, CreateSourceTemplate());

        destination.RotateBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, 2, CreateSourceTemplate());

        var rotated = destination.GetRegionPartByAbsolutePart(DestinationRegionX * 8 + DestinationLocalPartX, DestinationRegionY * 8 + DestinationLocalPartY, 0);
        var mirrored = destination.GetRegionPartByAbsolutePart(DestinationRegionX * 8 + DestinationLocalPartY, DestinationRegionY * 8 + DestinationLocalPartX, 0);
        Assert.AreEqual(2, rotated.Rotation);
        Assert.AreEqual(0, mirrored.Rotation);
        Assert.AreEqual(SourceObjects.Length, rotated.FindAllGameObjects().Count());

        destination.DeleteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0);

        Assert.IsFalse(rotated.HasDrawSource);
        Assert.IsFalse(rotated.FindAllGameObjects().Any());
        Assert.IsTrue(mirrored.HasDrawSource);
        Assert.AreEqual(SourceObjects.Length, mirrored.FindAllGameObjects().Count());
    }

    [TestMethod]
    public void Delete_MakesThePartAbsentAndClearsItsCollision()
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, CreateSourceTemplate());
        var registered = destination.FindAllGameObjects().ToList();

        destination.DeleteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0);

        var part = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0);
        Assert.IsFalse(part.HasDrawSource);
        Assert.IsFalse(destination.FindAllGameObjects().Any());
        Assert.IsTrue(Collision(destination).All(flag => flag == CollisionFlag.Walkable));
        Assert.IsTrue(registered.All(obj => !world.Store.TryResolve(((GameObject)obj).Handle, out _)));
    }

    [TestMethod]
    public void Write_DescriptorZeroIsAPresentMapping()
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        var originTemplate = new MapRegionTemplate(0,
            [new MapRegionTemplateObject(7, 2, 2, 0, 0, ShapeType.GroundDefault, Standard1X1, true)],
            []);

        destination.WriteBlockByIndex(0, 0, 0, 0, 0, 0, originTemplate);

        var part = destination.GetRegionPartByLocalPart(0, 0, 0);
        Assert.IsTrue(part.HasDrawSource);
        Assert.AreEqual(0, part.GetHashCode());
        Assert.AreEqual(7, destination.FindStandardGameObject(2, 2, 0)!.Id);
    }

    [TestMethod]
    public void Rewrite_DiscardsOldContentWithoutLeakingDisabledStatics()
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, CreateSourceTemplate());
        var part = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0);
        var removed = (GameObject)destination.FindAllGameObjects().Single(obj => obj.Id == 1);

        destination.Remove(removed);

        Assert.AreSame(removed, part.FindAllDisabledStaticGameObjects().Single());

        var emptyTemplate = new MapRegionTemplate(SourceRegionId, [], []);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX - 1, SourcePartY, SourceZ, emptyTemplate);

        Assert.IsFalse(part.FindAllDisabledStaticGameObjects().Any());
        Assert.IsFalse(part.FindAllGameObjects().Any());
        Assert.IsFalse(world.Store.TryResolve(removed.Handle, out _));
        Assert.IsTrue(Collision(destination).All(flag => flag == CollisionFlag.Walkable));
    }

    [TestMethod]
    public void ZoneUpdates_AddressTheDestinationPartForCrossPartAndCrossPlaneMappings()
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, CreateSourceTemplate());
        var part = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0);
        destination.Remove(destination.FindAllGameObjects().Single(obj => obj.Id == 1));
        var character = CreateObserver(out var bases, out var zoneMessages);

        part.SendFullUpdate(character);
        part.PrepareUpdatesForTick();
        part.SendUpdates(character);

        Assert.AreEqual(2, bases.Count);
        foreach (var zoneBase in bases)
        {
            Assert.AreEqual(DestinationRegionX * 64 + DestinationMinX, zoneBase.X);
            Assert.AreEqual(DestinationRegionY * 64 + DestinationMinY, zoneBase.Y);
            Assert.AreEqual(0, zoneBase.Z);
        }

        Assert.AreEqual(2, zoneMessages.Count);
        Assert.IsTrue(zoneMessages[0].FullUpdate);
        Assert.IsFalse(zoneMessages[1].FullUpdate);
        Assert.IsTrue(zoneMessages.All(message => message.Z == 0));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Rewrite_DropsQueuedUpdatesOfDiscardedObjectsButKeepsOtherUpdates(bool preparedBeforeRewrite)
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, CreateSourceTemplate());
        var part = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0);
        var removed = destination.FindAllGameObjects().Single(obj => obj.Id == 1);
        destination.Remove(removed);
        var kept = Substitute.For<IRegionPartUpdate>();
        kept.Location.Returns(Location.Create(DestinationRegionX * 64 + DestinationMinX + 3, DestinationRegionY * 64 + DestinationMinY + 3, 0, 0));
        kept.CanUpdateFor(Arg.Any<ICharacter>()).Returns(true);
        destination.QueueUpdate(kept);
        if (preparedBeforeRewrite)
        {
            part.PrepareUpdatesForTick();
        }

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX - 1, SourcePartY, SourceZ, new MapRegionTemplate(SourceRegionId, [], []));
        if (!preparedBeforeRewrite)
        {
            part.PrepareUpdatesForTick();
        }

        var character = CreateObserver(out _, out var zoneMessages);
        part.SendUpdates(character);

        Assert.AreEqual(1, zoneMessages.Count);
        kept.Received(1).OnUpdatedFor(character);
        removed.Script.DidNotReceive().OnDeletedFor(character);
    }

    [TestMethod]
    public void Delete_KeepsCollisionOfAnOverlappingObjectFromAnotherPart()
    {
        // A 2x3 object rooted at the east edge of part (5, 6) overlaps part (6, 6), where
        // another solid object shares one of its tiles.
        var template = new MapRegionTemplate(SourceRegionId,
        [
            new MapRegionTemplateObject(10, SourceMinX + 7, SourceMinY, SourceZ, 0, ShapeType.GroundDefault, Standard2X3, true),
            new MapRegionTemplateObject(11, SourceMinX + 8, SourceMinY + 1, SourceZ, 0, ShapeType.GroundDefault, Standard1X1, true)
        ], []);
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        var expected = world.CreateRegion(DestinationRegionX, DestinationRegionY, dimension: 1);
        expected.MakeDynamic();
        expected.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, template);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, template);
        destination.WriteBlockByIndex(DestinationLocalPartX + 1, DestinationLocalPartY, 0, SourcePartX + 1, SourcePartY, SourceZ, template);

        destination.DeleteBlockByIndex(DestinationLocalPartX + 1, DestinationLocalPartY, 0);

        Assert.IsTrue(destination.GetCollision(DestinationMinX + 8, DestinationMinY + 1, 0).HasFlag(CollisionFlag.ObjectBlock));
        CollectionAssert.AreEqual(Collision(expected), Collision(destination));
    }

    [TestMethod]
    public void MakeStandard_RestoresMappingsObjectsAndCollision()
    {
        var world = new TestWorld();
        var ownTemplate = new MapRegionTemplate(
            (DestinationRegionX << 8) | DestinationRegionY,
            SourceObjects.Select(obj => new MapRegionTemplateObject(obj.Id, DestinationMinX + obj.X, DestinationMinY + obj.Y, 0, obj.Rotation, obj.Shape, obj.Definition, true)),
            [new BlockedTerrainTile(DestinationMinX + SourceFloorBlock.X, DestinationMinY + SourceFloorBlock.Y, 0)]);
        var fresh = world.CreateRegion(DestinationRegionX, DestinationRegionY, dimension: 1);
        fresh.MakeStandard(ownTemplate);
        var region = world.CreateRegion(DestinationRegionX, DestinationRegionY);
        region.MakeStandard(ownTemplate);
        region.MakeDynamic();
        region.WriteBlockByIndex(0, 0, 0, SourcePartX, SourcePartY, SourceZ, CreateSourceTemplate());
        region.RotateBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, 3, ownTemplate);
        region.DeleteBlockByIndex(1, 1, 0);

        region.MakeStandard(ownTemplate);

        Assert.IsFalse(region.IsDynamic);
        for (var z = 0; z < 4; z++)
        {
            for (var x = 0; x < 8; x++)
            {
                for (var y = 0; y < 8; y++)
                {
                    var part = region.GetRegionPartByLocalPart(x, y, z);
                    Assert.IsTrue(part.HasDrawSource);
                    Assert.AreEqual(DestinationRegionX * 8 + x, part.DrawRegionPartX);
                    Assert.AreEqual(DestinationRegionY * 8 + y, part.DrawRegionPartY);
                    Assert.AreEqual(z, part.DrawRegionZ);
                    Assert.AreEqual(0, part.Rotation);
                    Assert.IsFalse(part.FindAllDisabledStaticGameObjects().Any());
                }
            }
        }

        CollectionAssert.AreEquivalent(ObjectSnapshot(fresh), ObjectSnapshot(region));
        CollectionAssert.AreEqual(Collision(fresh), Collision(region));
    }

    private static MapRegionTemplate CreateSourceTemplate(params MapRegionTemplateObject[] extra) => new(
        SourceRegionId,
        SourceObjects.Select(obj => new MapRegionTemplateObject(obj.Id, SourceMinX + obj.X, SourceMinY + obj.Y, SourceZ, obj.Rotation, obj.Shape, obj.Definition, true))
            .Concat(extra),
        [new BlockedTerrainTile(SourceMinX + SourceFloorBlock.X, SourceMinY + SourceFloorBlock.Y, SourceZ)]);

    private static IGameObjectDefinition Definition(int sizeX, int sizeY)
    {
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.SizeX.Returns(sizeX);
        definition.SizeY.Returns(sizeY);
        definition.ClipType.Returns(1);
        definition.Solid.Returns(true);
        definition.Gateway.Returns(false);
        return definition;
    }

    private static ICharacter CreateObserver(out List<ILocation> zoneBases, out List<MapRegionPartUpdateMessage> zoneMessages)
    {
        var bases = zoneBases = [];
        var messages = zoneMessages = [];
        var character = Substitute.For<ICharacter>();
        var viewport = Substitute.For<IViewport>();
        var session = Substitute.For<IGameSession>();
        character.Viewport.Returns(viewport);
        character.Session.Returns(session);
        viewport.InBounds(Arg.Any<ILocation>()).Returns(true);
        viewport.When(value => value.GetLocalPosition(Arg.Any<ILocation>(), ref Arg.Any<int>(), ref Arg.Any<int>()))
            .Do(call => bases.Add(call.ArgAt<ILocation>(0)));
        session.When(value => value.SendMessage(Arg.Any<RaidoMessage>()))
            .Do(call =>
            {
                if (call.Arg<RaidoMessage>() is MapRegionPartUpdateMessage message)
                {
                    messages.Add(message);
                }
            });
        return character;
    }

    private static (int, int) FootprintOf(IGameObject obj) =>
        (obj.Rotation & 0x1) == 1 ? (obj.Definition.SizeY, obj.Definition.SizeX) : (obj.Definition.SizeX, obj.Definition.SizeY);

    private static CollisionFlag[] Collision(IMapRegion region) =>
        Enumerable.Range(0, 4 * 64 * 64).Select(index => region.GetCollision((index / 64) % 64, index % 64, index / 4096)).ToArray();

    private static string[] ObjectSnapshot(IMapRegion region) =>
        region.FindAllGameObjects()
            .Select(obj => $"{obj.Id}:{obj.Location.RegionLocalX},{obj.Location.RegionLocalY},{obj.Location.Z}:{obj.Rotation}:{obj.ShapeType}:{obj.IsStatic}")
            .ToArray();

    private sealed class TestWorld
    {
        private readonly Dictionary<(int Id, int Dimension), MapRegion> _regions = new();
        private readonly IMapRegionService _regionService = Substitute.For<IMapRegionService>();

        public IMapRegionService RegionService => _regionService;
        private readonly GameObjectBuilder _builder;

        public TestWorld()
        {
            var scriptProvider = Substitute.For<IGameObjectScriptProvider>();
            scriptProvider.GetGameObjectScriptTypeById(Arg.Any<int>()).Returns(typeof(IGameObjectScript));
            var services = new ServiceCollection().AddSingleton(_ => Substitute.For<IGameObjectScript>()).BuildServiceProvider();
            _builder = new GameObjectBuilder(services, scriptProvider);
            _regionService.When(service => service.FlagCollision(Arg.Any<ILocation>(), Arg.Any<CollisionFlag>()))
                .Do(call => Route(call.Arg<ILocation>()!, call.Arg<CollisionFlag>(), (region, x, y, z, flag) => region.FlagCollision(x, y, z, flag)));
            _regionService.When(service => service.UnFlagCollision(Arg.Any<ILocation>(), Arg.Any<CollisionFlag>()))
                .Do(call => Route(call.Arg<ILocation>()!, call.Arg<CollisionFlag>(), (region, x, y, z, flag) => region.UnFlagCollision(x, y, z, flag)));
        }

        public EntityStore Store { get; } = new();

        public MapRegion CreateRegion(int regionX, int regionY, int dimension = 0)
        {
            var region = new MapRegion(Location.Create(regionX * 64, regionY * 64, 0, dimension), [0, 0, 0, 0],
                Substitute.For<INpcService>(), _regionService, _builder, Substitute.For<IGroundItemBuilder>(),
                Substitute.For<IMapper>(), Store);
            _regions.Add((region.Id, dimension), region);
            return region;
        }

        public MapRegion CreateDynamicRegion(int regionX, int regionY)
        {
            var region = CreateRegion(regionX, regionY);
            region.MakeDynamic();
            return region;
        }

        public static GameObject CreateGameObject(int id, int x, int y, int z, int dimension, int rotation, ShapeType shape, IGameObjectDefinition definition) =>
            new(id, Location.Create(x, y, z, dimension), rotation, shape, true, definition, Substitute.For<IGameObjectScript>());

        private void Route(ILocation location, CollisionFlag flag, Action<MapRegion, int, int, int, CollisionFlag> action)
        {
            if (_regions.TryGetValue((location.RegionId, location.Dimension), out var region))
            {
                action(region, location.RegionLocalX, location.RegionLocalY, location.Z, flag);
            }
        }
    }
}
