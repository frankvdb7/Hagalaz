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

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(CreateSourceTemplate()));
        destination.RotateBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, rotation, Templates(CreateSourceTemplate()));

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

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(CreateSourceTemplate()));
        destination.RotateBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, rotation, Templates(CreateSourceTemplate()));

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

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(template));

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

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(CreateSourceTemplate()));

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
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(CreateSourceTemplate()));

        Assert.ThrowsExactly<ArgumentException>(() => destination.WriteBlockByIndex(
            DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX + 8, SourcePartY, SourceZ, Templates(CreateSourceTemplate())));

        Assert.AreEqual(SourcePartX, destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0).DrawRegionPartX);
        Assert.AreEqual(SourceObjects.Length, destination.FindAllGameObjects().Count());
    }

    [TestMethod]
    public void RotateAndDelete_UseTheLocalPartYConversion()
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(CreateSourceTemplate()));
        destination.WriteBlockByIndex(DestinationLocalPartY, DestinationLocalPartX, 0, SourcePartX, SourcePartY, SourceZ, Templates(CreateSourceTemplate()));

        destination.RotateBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, 2, Templates(CreateSourceTemplate()));

        var rotated = destination.GetRegionPartByAbsolutePart(DestinationRegionX * 8 + DestinationLocalPartX, DestinationRegionY * 8 + DestinationLocalPartY, 0);
        var mirrored = destination.GetRegionPartByAbsolutePart(DestinationRegionX * 8 + DestinationLocalPartY, DestinationRegionY * 8 + DestinationLocalPartX, 0);
        Assert.AreEqual(2, rotated.Rotation);
        Assert.AreEqual(0, mirrored.Rotation);
        Assert.AreEqual(SourceObjects.Length, rotated.FindAllGameObjects().Count());

        destination.DeleteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, Templates(CreateSourceTemplate()));

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
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(CreateSourceTemplate()));
        var registered = destination.FindAllGameObjects().ToList();

        destination.DeleteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, Templates(CreateSourceTemplate()));

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

        destination.WriteBlockByIndex(0, 0, 0, 0, 0, 0, Templates(originTemplate));

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
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(CreateSourceTemplate()));
        var part = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0);
        var removed = (GameObject)destination.FindAllGameObjects().Single(obj => obj.Id == 1);

        destination.Remove(removed);

        Assert.AreSame(removed, part.FindAllDisabledStaticGameObjects().Single());

        var emptyTemplate = new MapRegionTemplate(SourceRegionId, [], []);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX - 1, SourcePartY, SourceZ, Templates(emptyTemplate));

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
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(CreateSourceTemplate()));
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
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(CreateSourceTemplate()));
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

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX - 1, SourcePartY, SourceZ, Templates(new MapRegionTemplate(SourceRegionId, [], [])));
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
        var template = CreateOverlapTemplate();
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        var expected = world.CreateRegion(DestinationRegionX, DestinationRegionY, dimension: 1);
        expected.MakeDynamic();
        expected.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(template));
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(template));
        destination.WriteBlockByIndex(DestinationLocalPartX + 1, DestinationLocalPartY, 0, SourcePartX + 1, SourcePartY, SourceZ, Templates(template));

        destination.DeleteBlockByIndex(DestinationLocalPartX + 1, DestinationLocalPartY, 0, Templates(CreateSourceTemplate()));

        Assert.IsTrue(destination.GetCollision(DestinationMinX + 8, DestinationMinY + 1, 0).HasFlag(CollisionFlag.ObjectBlock));
        CollectionAssert.AreEqual(Collision(expected), Collision(destination));
    }

    [TestMethod]
    public void Delete_KeepsCollisionOfAnOverlappingObjectFromAnAdjacentRegion()
    {
        // The 2x3 object is rooted at region A local X 63 and overlaps region B local X 0,
        // where a solid object rooted in B shares a tile with it.
        var template = CreateOverlapTemplate();
        var world = new TestWorld();
        var regionA = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        var regionB = world.CreateDynamicRegion(DestinationRegionX + 1, DestinationRegionY);
        var expectedB = world.CreateRegion(DestinationRegionX + 1, DestinationRegionY, dimension: 1);
        expectedB.MakeDynamic();
        expectedB.WriteBlockByIndex(0, DestinationLocalPartY, 0, SourcePartX + 1, SourcePartY, SourceZ, Templates(template));
        regionA.WriteBlockByIndex(7, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(template));
        regionB.WriteBlockByIndex(0, DestinationLocalPartY, 0, SourcePartX + 1, SourcePartY, SourceZ, Templates(template));
        Assert.IsFalse(regionB.GetCollision(1, DestinationMinY, 0).HasFlag(CollisionFlag.ObjectBlock));
        Assert.IsTrue(regionB.GetCollision(0, DestinationMinY, 0).HasFlag(CollisionFlag.ObjectBlock), "A's footprint reaches region B.");

        regionA.DeleteBlockByIndex(7, DestinationLocalPartY, 0, Templates(CreateSourceTemplate()));

        Assert.IsTrue(regionB.GetCollision(0, DestinationMinY + 1, 0).HasFlag(CollisionFlag.ObjectBlock));
        CollectionAssert.AreEqual(Collision(expectedB), Collision(regionB));
    }

    [TestMethod]
    public void Delete_RemovesRuntimeObjectsButKeepsObjectsPlacedByOtherColumns()
    {
        // Column (6, 6) copies the source chunk rotated by 2, which roots its 2x3 object (10)
        // in column (5, 6) at local (47, 53). Column (5, 6) copies an empty chunk.
        var template = CreateSourceTemplate(new MapRegionTemplateObject(10, SourceMinX + 7, SourceMinY, SourceZ, 0, ShapeType.GroundDefault, Standard2X3, true));
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlocksByIndex(
        [
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX - 1, SourcePartY, SourceZ, 0),
            new MapRegionPartMapping(DestinationLocalPartX + 1, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, 2)
        ], Templates(template));
        var foreign = destination.FindStandardGameObject(47, 53, 0);
        Assert.AreEqual(10, foreign?.Id);
        var runtimeObject = TestWorld.CreateGameObject(77, DestinationRegionX * 64 + DestinationMinX + 3, DestinationRegionY * 64 + DestinationMinY + 3, 0, 0, 0,
            ShapeType.GroundDefault, Standard1X1, isStatic: false);
        var disabledStatic = TestWorld.CreateGameObject(78, DestinationRegionX * 64 + DestinationMinX + 2, DestinationRegionY * 64 + DestinationMinY + 1, 0, 0, 0,
            ShapeType.GroundDefault, Standard1X1);
        destination.Add(runtimeObject);
        destination.Add(disabledStatic);
        destination.Remove(disabledStatic);
        var part = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0);
        Assert.AreSame(disabledStatic, part.FindAllDisabledStaticGameObjects().Single());

        destination.DeleteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, Templates(template));

        Assert.IsFalse(destination.FindGameObjects(DestinationMinX + 3, DestinationMinY + 3, 0).Any());
        Assert.IsFalse(part.FindAllDisabledStaticGameObjects().Any());
        Assert.AreEqual(CollisionFlag.Walkable, destination.GetCollision(DestinationMinX + 3, DestinationMinY + 3, 0));
        Assert.IsFalse(world.Store.TryResolve(runtimeObject.Handle, out _));
        Assert.IsFalse(world.Store.TryResolve(disabledStatic.Handle, out _));
        Assert.AreSame(foreign, destination.FindStandardGameObject(47, 53, 0));
        Assert.IsTrue(destination.GetCollision(47, 53, 0).HasFlag(CollisionFlag.ObjectBlock));
    }

    [TestMethod]
    public void PlaneZeroObjectUnderABridge_IsKeptWithoutCollisionUntilTheBridgeGoes()
    {
        // Map.readRegionPartLandscape still draws the object when the destination tile's
        // collision plane is below 0; it only skips clipping.
        var template = CreateTemplateWithTerrain(
            [new MapRegionTemplateObject(30, SourceMinX + 1, SourceMinY + 1, 0, 0, ShapeType.GroundDefault, Standard1X1, true)],
            terrain => terrain[1, SourceMinX + 1, SourceMinY + 1] = 0x2);
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);

        destination.WriteBlocksByIndex(
        [
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, 0, 0),
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX, SourcePartY, 1, 0)
        ], Templates(template));

        var drawn = destination.FindStandardGameObject(DestinationMinX + 1, DestinationMinY + 1, 0);
        Assert.AreEqual(30, drawn?.Id);
        Assert.IsTrue(Collision(destination).All(flag => flag == CollisionFlag.Walkable));

        destination.DeleteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 1, Templates(template));

        Assert.AreEqual(30, destination.FindStandardGameObject(DestinationMinX + 1, DestinationMinY + 1, 0)?.Id);
        Assert.IsTrue(destination.GetCollision(DestinationMinX + 1, DestinationMinY + 1, 0).HasFlag(CollisionFlag.ObjectBlock));
    }

    [TestMethod]
    public void ChangingABridgeChunk_RebuildsColumnsThatReadIt()
    {
        // Rotation 2 roots the 2x1 object of source chunk (2, 3) at destination local (39, 53),
        // in column (4, 6), whose plane-1 chunk is all bridge.
        var template = CreateTemplateWithTerrain(
            [new MapRegionTemplateObject(20, SourceMinX + 7, SourceMinY + 2, 0, 0, ShapeType.GroundDefault, Definition(2, 1), true)],
            terrain =>
            {
                for (var x = 0; x < 8; x++)
                {
                    for (var y = 0; y < 8; y++)
                    {
                        terrain[1, SourceMinX + 8 + x, SourceMinY + y] = 0x2;
                    }
                }
            });
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlocksByIndex(
        [
            new MapRegionPartMapping(DestinationLocalPartX - 1, DestinationLocalPartY, 1, SourcePartX + 1, SourcePartY, 1, 0),
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, 0, 2)
        ], Templates(template));
        Assert.AreEqual(20, destination.FindStandardGameObject(39, 53, 0)?.Id);
        Assert.AreEqual(CollisionFlag.Walkable, destination.GetCollision(39, 53, 0));

        destination.DeleteBlockByIndex(DestinationLocalPartX - 1, DestinationLocalPartY, 1, Templates(template));

        Assert.AreEqual(20, destination.FindStandardGameObject(39, 53, 0)?.Id);
        Assert.IsTrue(destination.GetCollision(39, 53, 0).HasFlag(CollisionFlag.ObjectBlock));
        Assert.IsTrue(destination.GetCollision(40, 53, 0).HasFlag(CollisionFlag.ObjectBlock));
    }

    [TestMethod]
    public void ReplacingAnObject_ClearsTheReplacedFootprintUntilTheChunkIsDeleted()
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        var expected = world.CreateRegion(DestinationRegionX, DestinationRegionY, dimension: 1);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(CreateSourceTemplate()));
        // The 1x1 replaces the 2x3 object (source index 1) rooted on the same tile and layer.
        var (rootX, rootY) = ExpectedObjectPositions[0][1];
        destination.Add(TestWorld.CreateGameObject(90, DestinationRegionX * 64 + DestinationMinX + rootX, DestinationRegionY * 64 + DestinationMinY + rootY, 0, 0, 0,
            ShapeType.GroundDefault, Standard1X1));
        for (var index = 0; index < SourceObjects.Length; index++)
        {
            var source = SourceObjects[index];
            var (x, y) = ExpectedObjectPositions[0][index];
            expected.Add(index == 1
                ? TestWorld.CreateGameObject(90, DestinationRegionX * 64 + DestinationMinX + x, DestinationRegionY * 64 + DestinationMinY + y, 0, 1, 0, ShapeType.GroundDefault, Standard1X1)
                : TestWorld.CreateGameObject(source.Id, DestinationRegionX * 64 + DestinationMinX + x, DestinationRegionY * 64 + DestinationMinY + y, 0, 1, source.Rotation, source.Shape, source.Definition));
        }

        expected.FlagCollision(DestinationMinX + SourceFloorBlock.X, DestinationMinY + SourceFloorBlock.Y, 0, CollisionFlag.FloorBlock);

        CollectionAssert.AreEqual(Collision(expected), Collision(destination));

        destination.DeleteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, Templates(CreateSourceTemplate()));

        Assert.IsTrue(Collision(destination).All(flag => flag == CollisionFlag.Walkable));
    }

    [TestMethod]
    public void SpawnHookThatRemovesTheObject_LeavesNoCollisionAndKeepsOverlaps()
    {
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        var expected = world.CreateRegion(DestinationRegionX, DestinationRegionY, dimension: 1);
        var baseX = DestinationRegionX * 64;
        var baseY = DestinationRegionY * 64;
        // A 2x3 object rooted at (10, 10) covers (11, 11), the tile of the self-removing object.
        destination.Add(TestWorld.CreateGameObject(91, baseX + 10, baseY + 10, 0, 0, 0, ShapeType.GroundDefault, Standard2X3));
        expected.Add(TestWorld.CreateGameObject(91, baseX + 10, baseY + 10, 0, 1, 0, ShapeType.GroundDefault, Standard2X3));
        GameObject CreateSelfRemoving(int id, int localX, int localY)
        {
            var script = Substitute.For<IGameObjectScript>();
            GameObject? self = null;
            script.When(value => value.OnSpawn()).Do(_ => destination.Remove(self!));
            self = new GameObject(id, Location.Create(baseX + localX, baseY + localY, 0, 0), 0, ShapeType.GroundDefault, false, Standard1X1, script);
            return self;
        }

        destination.Add(CreateSelfRemoving(92, 11, 11));
        destination.Add(CreateSelfRemoving(93, 20, 20));

        Assert.IsFalse(destination.FindAllGameObjects().Any(obj => obj.Id is 92 or 93));
        Assert.IsTrue(destination.GetCollision(11, 11, 0).HasFlag(CollisionFlag.ObjectBlock));
        Assert.AreEqual(CollisionFlag.Walkable, destination.GetCollision(20, 20, 0));
        CollectionAssert.AreEqual(Collision(expected), Collision(destination));
    }

    [TestMethod]
    public void DeletingAChunkThatDisplacedAnObject_RestoresTheDisplacedObject()
    {
        // A: column (5, 6) maps source chunk (2, 3), whose 1x1 object at chunk (7, 7) lands on (47, 55).
        // B: column (6, 6) maps source chunk (3, 3) rotated by 2, whose 2x1 object at chunk (7, 0)
        // also roots at (47, 55) and replaces A's object in that slot.
        var template = new MapRegionTemplate(SourceRegionId,
        [
            new MapRegionTemplateObject(50, SourceMinX + 7, SourceMinY + 7, SourceZ, 0, ShapeType.GroundDefault, Standard1X1, true),
            new MapRegionTemplateObject(51, SourceMinX + 8 + 7, SourceMinY, SourceZ, 0, ShapeType.GroundDefault, Definition(2, 1), true)
        ], []);
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        var expected = world.CreateRegion(DestinationRegionX, DestinationRegionY, dimension: 1);
        expected.MakeDynamic();
        expected.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(template));

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(template));
        destination.WriteBlockByIndex(DestinationLocalPartX + 1, DestinationLocalPartY, 0, SourcePartX + 1, SourcePartY, SourceZ, Templates(template));
        destination.RotateBlockByIndex(DestinationLocalPartX + 1, DestinationLocalPartY, 0, 2, Templates(template));
        Assert.AreEqual(51, destination.FindStandardGameObject(47, 55, 0)?.Id);

        destination.DeleteBlockByIndex(DestinationLocalPartX + 1, DestinationLocalPartY, 0, Templates(template));

        Assert.AreEqual(50, destination.FindStandardGameObject(47, 55, 0)?.Id);
        CollectionAssert.AreEqual(Collision(expected), Collision(destination));
    }

    [TestMethod]
    public void RewritingTheLosingChunk_KeepsTheWinnersObjectWhateverTheEditOrder()
    {
        var template = CreateSharedSlotTemplate();
        var world = new TestWorld();
        var expected = world.CreateRegion(DestinationRegionX, DestinationRegionY, dimension: 1);
        expected.MakeDynamic();
        expected.WriteBlocksByIndex(SharedSlotMappings(), Templates(template));
        var losingFirst = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        var winningFirst = world.CreateRegion(DestinationRegionX, DestinationRegionY, dimension: 2);
        winningFirst.MakeDynamic();

        losingFirst.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(template));
        losingFirst.WriteBlockByIndex(DestinationLocalPartX + 1, DestinationLocalPartY, 0, SourcePartX + 1, SourcePartY, SourceZ, Templates(template));
        losingFirst.RotateBlockByIndex(DestinationLocalPartX + 1, DestinationLocalPartY, 0, 2, Templates(template));
        losingFirst.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(template));
        winningFirst.WriteBlocksByIndex([SharedSlotMappings()[1]], Templates(template));
        winningFirst.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(template));

        Assert.AreEqual(51, losingFirst.FindStandardGameObject(47, 55, 0)?.Id);
        Assert.AreEqual(51, winningFirst.FindStandardGameObject(47, 55, 0)?.Id);
        CollectionAssert.AreEqual(Collision(expected), Collision(losingFirst));
        CollectionAssert.AreEqual(Collision(expected), Collision(winningFirst));
    }

    [TestMethod]
    public void RewritingTheLosingChunk_KeepsARuntimeRemovedWinnerRemoved()
    {
        var template = CreateSharedSlotTemplate();
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlocksByIndex(SharedSlotMappings(), Templates(template));
        var winner = destination.FindStandardGameObject(47, 55, 0)!;
        Assert.AreEqual(51, winner.Id);
        destination.Remove(winner);
        var winnerPart = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0);

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(template));

        Assert.IsNull(destination.FindStandardGameObject(47, 55, 0));
        Assert.AreSame(winner, winnerPart.FindAllDisabledStaticGameObjects().Single());
        Assert.AreEqual(CollisionFlag.Walkable, destination.GetCollision(47, 55, 0));
    }

    [TestMethod]
    public void EditingPlaneZero_LeavesARuntimeRemovalOnPlaneOneAlone()
    {
        var (destination, template) = CreateTwoPlaneRegion();
        var (x, y) = ExpectedObjectPositions[0][0];
        var removed = destination.FindStandardGameObject(DestinationMinX + x, DestinationMinY + y, 1)!;
        destination.Remove(removed);
        var planeOne = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 1);
        planeOne.PrepareUpdatesForTick();
        planeOne.CompleteUpdateTick();
        var planeOneObjects = planeOne.FindAllGameObjects().ToList();
        var planeOneCollision = Collision(destination).Skip(4096).Take(4096).ToArray();

        destination.RotateBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, 1, Templates(template));

        Assert.AreSame(removed, planeOne.FindAllDisabledStaticGameObjects().Single());
        Assert.IsNull(destination.FindStandardGameObject(DestinationMinX + x, DestinationMinY + y, 1));
        CollectionAssert.AreEqual(planeOneObjects, planeOne.FindAllGameObjects().ToList());
        CollectionAssert.AreEqual(planeOneCollision, Collision(destination).Skip(4096).Take(4096).ToArray());
        var character = CreateObserver(out _, out var zoneMessages);
        planeOne.PrepareUpdatesForTick();
        planeOne.SendUpdates(character);
        Assert.AreEqual(0, zoneMessages.Count);
    }

    [TestMethod]
    public void RewritingPlaneOneUnchanged_LeavesPlaneZeroRuntimeRemovalAlone()
    {
        var (destination, template) = CreateTwoPlaneRegion();
        var (x, y) = ExpectedObjectPositions[0][0];
        var removed = destination.FindStandardGameObject(DestinationMinX + x, DestinationMinY + y, 0)!;
        destination.Remove(removed);
        var planeZero = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0);
        planeZero.PrepareUpdatesForTick();
        planeZero.CompleteUpdateTick();
        var collision = Collision(destination);

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX, SourcePartY, SourceZ, Templates(template));

        Assert.AreSame(removed, planeZero.FindAllDisabledStaticGameObjects().Single());
        Assert.IsNull(destination.FindStandardGameObject(DestinationMinX + x, DestinationMinY + y, 0));
        CollectionAssert.AreEqual(collision, Collision(destination));
        var character = CreateObserver(out _, out var zoneMessages);
        planeZero.PrepareUpdatesForTick();
        planeZero.SendUpdates(character);
        Assert.AreEqual(0, zoneMessages.Count);
    }

    [TestMethod]
    public void BeatenChunk_KeepsItsRuntimeRemovalWhenTheWinnerIsDeleted()
    {
        var template = CreateSharedSlotTemplate();
        var world = new TestWorld();
        var destination = world.CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, Templates(template));
        var loser = destination.FindStandardGameObject(47, 55, 0)!;
        Assert.AreEqual(50, loser.Id);
        destination.Remove(loser);

        destination.WriteBlocksByIndex([SharedSlotMappings()[1]], Templates(template));
        Assert.AreEqual(51, destination.FindStandardGameObject(47, 55, 0)?.Id);
        destination.DeleteBlockByIndex(DestinationLocalPartX + 1, DestinationLocalPartY, 0, Templates(template));

        Assert.IsNull(destination.FindStandardGameObject(47, 55, 0));
        Assert.AreEqual(CollisionFlag.Walkable, destination.GetCollision(47, 55, 0));
    }

    [TestMethod]
    public void BridgeDependencyRebuild_KeepsARemovedConfiguredSpawnRemoved()
    {
        var (destination, planeZero, staticObject, spawnX, spawnY, templates) = CreateBridgeDependencyScenario(isStatic: false);
        var spawn = destination.FindStandardGameObject(spawnX, spawnY, 0)!;
        destination.Remove(spawn);

        ChangeBridgeAwayFromTheObjects(destination, templates);

        Assert.IsNull(destination.FindStandardGameObject(spawnX, spawnY, 0));
        Assert.AreEqual(CollisionFlag.Walkable, destination.GetCollision(spawnX, spawnY, 0));
        Assert.AreSame(staticObject, planeZero.FindAllGameObjects().Single(obj => obj.Id == 70));
    }

    [TestMethod]
    public void BridgeDependencyRebuild_KeepsAnObjectChangedInPlace()
    {
        var (destination, _, staticObject, spawnX, spawnY, templates) = CreateBridgeDependencyScenario(isStatic: true);
        var changed = (GameObject)destination.FindStandardGameObject(spawnX, spawnY, 0)!;
        changed.Id = 734;

        ChangeBridgeAwayFromTheObjects(destination, templates);

        Assert.AreSame(changed, destination.FindStandardGameObject(spawnX, spawnY, 0));
        Assert.AreEqual(734, changed.Id);
        Assert.IsTrue(destination.GetCollision(spawnX, spawnY, 0).HasFlag(CollisionFlag.ObjectBlock));
        Assert.AreSame(staticObject, destination.FindAllGameObjects().Single(obj => obj.Id == 70));
    }

    // Plane 0 and plane 1 map the same bridge-free chunk. Plane 0 holds a static object U (id 70) and
    // a second object at (3, 3) that is configured (not static) or a static cache object.
    private static (MapRegion Destination, IMapRegionPart PlaneZero, IGameObject StaticObject, int SpawnX, int SpawnY, IReadOnlyDictionary<int, MapRegionTemplate> Templates) CreateBridgeDependencyScenario(bool isStatic)
    {
        var flags = new sbyte[4, 64, 64];
        flags[1, SourceMinX + 8 + 6, SourceMinY + 6] = 0x2;
        var template = new MapRegionTemplate(SourceRegionId, [],
            [],
            [
                new MapRegionTemplateObject(70, SourceMinX + 1, SourceMinY + 1, SourceZ, 0, ShapeType.GroundDefault, Standard1X1, true),
                new MapRegionTemplateObject(733, SourceMinX + 3, SourceMinY + 3, SourceZ, 0, ShapeType.GroundDefault, Standard1X1, isStatic)
            ], flags);
        var templates = Templates(template);
        var destination = new TestWorld().CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlocksByIndex(
        [
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, 0),
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX, SourcePartY, SourceZ, 0)
        ], templates);
        var planeZero = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0);
        var staticObject = planeZero.FindAllGameObjects().Single(obj => obj.Id == 70);
        return (destination, planeZero, staticObject, DestinationMinX + 3, DestinationMinY + 3, templates);
    }

    // Maps plane 1 to the neighbouring chunk, whose bridge flag at (6, 6) differs, away from the objects.
    private static void ChangeBridgeAwayFromTheObjects(MapRegion destination, IReadOnlyDictionary<int, MapRegionTemplate> templates) =>
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX + 1, SourcePartY, SourceZ, templates);

    [TestMethod]
    public void NewBridgeUnderAnObject_KeepsItDrawnWithoutCollision()
    {
        var (destination, templates) = CreateBridgeFreeObjectRegion();
        Assert.IsTrue(destination.GetCollision(DestinationMinX + 1, DestinationMinY + 1, 0).HasFlag(CollisionFlag.ObjectBlock));

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX + 1, SourcePartY, SourceZ, templates);

        Assert.AreEqual(70, destination.FindStandardGameObject(DestinationMinX + 1, DestinationMinY + 1, 0)?.Id);
        Assert.IsTrue(Collision(destination).All(flag => flag == CollisionFlag.Walkable));
    }

    [TestMethod]
    public void NewBridgeUnderARemovedObject_KeepsItRemoved()
    {
        var (destination, templates) = CreateBridgeFreeObjectRegion();
        var removed = destination.FindStandardGameObject(DestinationMinX + 1, DestinationMinY + 1, 0)!;
        destination.Remove(removed);
        var planeZero = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0);
        planeZero.PrepareUpdatesForTick();
        planeZero.CompleteUpdateTick();

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX + 1, SourcePartY, SourceZ, templates);

        Assert.IsNull(destination.FindStandardGameObject(DestinationMinX + 1, DestinationMinY + 1, 0));
        Assert.AreSame(removed, planeZero.FindAllDisabledStaticGameObjects().Single());
        Assert.IsTrue(Collision(destination).All(flag => flag == CollisionFlag.Walkable));
        var character = CreateObserver(out _, out var zoneMessages);
        planeZero.PrepareUpdatesForTick();
        planeZero.SendUpdates(character);
        Assert.AreEqual(0, zoneMessages.Count);
        zoneMessages.Clear();
        planeZero.SendFullUpdate(character);
        Assert.AreEqual(1, zoneMessages.Count, "A full update still sends the removal, and no add.");
    }

    [TestMethod]
    public void BridgeUnderTheWinningStatic_LetsTheCollidableConfiguredSpawnTakeTheSlot()
    {
        // A (column (5, 6)) is a configured spawn at (47, 55). B (column (6, 6), rotated by 2, so it loads
        // later) is a static whose 2x1 object roots on the same slot and wins it while both have collision.
        var templates = CreateSpawnAndRotatedStaticTemplates();
        var destination = new TestWorld().CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlocksByIndex(
        [
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, 0),
            new MapRegionPartMapping(DestinationLocalPartX + 1, DestinationLocalPartY, 0, SourcePartX + 1, SourcePartY, SourceZ, 2),
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX, SourcePartY, SourceZ, 0)
        ], templates);
        Assert.AreEqual(51, destination.FindStandardGameObject(47, 55, 0)?.Id);

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX + 1, SourcePartY, SourceZ, templates);

        Assert.AreEqual(50, destination.FindStandardGameObject(47, 55, 0)?.Id);
        Assert.IsTrue(destination.GetCollision(47, 55, 0).HasFlag(CollisionFlag.ObjectBlock));
    }

    [TestMethod]
    public void BridgeRoundTrip_KeepsAScriptUnclippedObjectUnclippedUntilTheScriptRestoresIt()
    {
        // The Web script cuts a web: it changes the id in place and unflags its collision, then restores
        // both after 100 ticks. A bridge appearing and going away in between must not re-close it.
        var (destination, templates) = CreateBridgeFreeObjectRegion();
        var web = (GameObject)destination.FindStandardGameObject(DestinationMinX + 1, DestinationMinY + 1, 0)!;
        web.Id = 734;
        destination.UnFlagCollision(web);
        Assert.IsTrue(Collision(destination).Take(4096).All(flag => flag == CollisionFlag.Walkable), "Plane 0 is walkable.");

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX + 1, SourcePartY, SourceZ, templates);
        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX, SourcePartY, SourceZ, templates);

        Assert.AreSame(web, destination.FindStandardGameObject(DestinationMinX + 1, DestinationMinY + 1, 0));
        Assert.AreEqual(734, web.Id);
        Assert.IsTrue(Collision(destination).Take(4096).All(flag => flag == CollisionFlag.Walkable), "Plane 0 is walkable.");

        web.Id = 733;
        destination.FlagCollision(web);

        Assert.IsTrue(destination.GetCollision(DestinationMinX + 1, DestinationMinY + 1, 0).HasFlag(CollisionFlag.ObjectBlock));
    }

    [TestMethod]
    public void ReplacingAScriptUnclippedObject_DoesNotKeepReferencingTheDestroyedOne()
    {
        var world = new TestWorld();
        var region = world.CreateRegion(DestinationRegionX, DestinationRegionY);
        var x = DestinationRegionX * 64 + 10;
        var y = DestinationRegionY * 64 + 10;
        var web = TestWorld.CreateGameObject(733, x, y, 0, 0, 0, ShapeType.GroundDefault, Standard1X1, isStatic: false);
        region.Add(web);
        region.UnFlagCollision(web);
        Assert.AreEqual(CollisionFlag.Walkable, region.GetCollision(10, 10, 0));

        var replacement = TestWorld.CreateGameObject(900, x, y, 0, 0, 0, ShapeType.GroundDefault, Standard1X1, isStatic: false);
        region.Add(replacement);

        Assert.AreSame(replacement, region.FindStandardGameObject(10, 10, 0));
        Assert.IsTrue(region.GetCollision(10, 10, 0).HasFlag(CollisionFlag.ObjectBlock));
        foreach (var field in new[] { "_unclippedObjects", "_objectsWithoutCollision" })
        {
            var set = (HashSet<IGameObject>)typeof(MapRegion)
                .GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .GetValue(region)!;
            Assert.IsFalse(set.Contains(web), $"{field} still references the replaced object.");
        }
    }

    [TestMethod]
    public void StaticMovingOffAPlane_FreesItsOldSlotForALowerRankedSpawn()
    {
        // A: configured spawn from column (5, 6) plane 0, at (47, 55). B: plane-1 chunk of column (6, 6),
        // rotated by 2, whose static 2x1 object roots on that tile in column (5, 6). While column (5, 6)
        // has a bridge on plane 1 there, B is placed on plane 0 and beats A; without it B sits on plane 1.
        var (destination, templates) = CreateBridgeMovedStaticRegion();
        Assert.AreEqual(51, destination.FindStandardGameObject(47, 55, 0)?.Id);

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX, SourcePartY, SourceZ, templates);

        Assert.AreEqual(50, destination.FindStandardGameObject(47, 55, 0)?.Id);
        Assert.AreEqual(51, destination.FindStandardGameObject(47, 55, 1)?.Id);
        Assert.IsTrue(destination.GetCollision(47, 55, 0).HasFlag(CollisionFlag.ObjectBlock));
    }

    [TestMethod]
    public void RemovedStaticMovingOffAPlane_StaysRemovedOnBothPlanes()
    {
        var (destination, templates) = CreateBridgeMovedStaticRegion();
        var removed = destination.FindStandardGameObject(47, 55, 0)!;
        Assert.AreEqual(51, removed.Id);
        destination.Remove(removed);
        var planeZero = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0);
        var planeOne = destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 1);

        // Bridge off, back on, off: the removed object is the same one each time and sits on the plane
        // it currently belongs to (the part storing its removal for clients), freeing or taking plane 0.
        foreach (var bridgeOff in new[] { true, false, true })
        {
            destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 1, bridgeOff ? SourcePartX : SourcePartX + 1, SourcePartY, SourceZ, templates);

            Assert.AreNotEqual(51, destination.FindStandardGameObject(47, 55, 0)?.Id);
            Assert.AreNotEqual(51, destination.FindStandardGameObject(47, 55, 1)?.Id);
            Assert.AreEqual(bridgeOff ? 50 : 0, destination.FindStandardGameObject(47, 55, 0)?.Id ?? 0, "The spawn B displaced returns while B is on plane 1.");
            Assert.AreSame(removed, (bridgeOff ? planeOne : planeZero).FindAllDisabledStaticGameObjects().Single());
            Assert.IsFalse((bridgeOff ? planeZero : planeOne).FindAllDisabledStaticGameObjects().Any(obj => obj.Id == 51));
        }

        // The scheduled respawn re-adds the original object on the plane it now belongs to.
        Assert.AreEqual(1, removed.Location.Z);
        destination.Add(removed);
        Assert.AreSame(removed, destination.FindStandardGameObject(47, 55, 1));
        Assert.AreEqual(50, destination.FindStandardGameObject(47, 55, 0)?.Id);
        Assert.IsTrue(destination.GetCollision(47, 55, 0).HasFlag(CollisionFlag.ObjectBlock));
    }

    [TestMethod]
    public void RemovedStaticMovingOntoAnUnmappedPlane_IsRelocatedAndCanRespawn()
    {
        // Only plane 1 is mapped: the rotated static chunk and the root column's bridge source (bridge off at first).
        var templates = CreateSpawnAndRotatedStaticTemplates();
        var destination = new TestWorld().CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlocksByIndex(
        [
            new MapRegionPartMapping(DestinationLocalPartX + 1, DestinationLocalPartY, 1, SourcePartX + 1, SourcePartY, SourceZ, 2),
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX, SourcePartY, SourceZ, 0)
        ], templates);
        var removed = destination.FindStandardGameObject(47, 55, 1)!;
        Assert.AreEqual(51, removed.Id);
        destination.Remove(removed);

        destination.WriteBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX + 1, SourcePartY, SourceZ, templates);

        Assert.AreEqual(0, removed.Location.Z);
        Assert.AreSame(removed, destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 0).FindAllDisabledStaticGameObjects().Single());
        Assert.IsFalse(destination.GetRegionPartByLocalPart(DestinationLocalPartX, DestinationLocalPartY, 1).FindAllDisabledStaticGameObjects().Any());
        destination.Add(removed);
        Assert.AreSame(removed, destination.FindStandardGameObject(47, 55, 0));
    }

    [TestMethod]
    public void FindGameObjectsOverlapping_LooksOnlyAtPartsThatCanHoldAnOverlappingObject()
    {
        var region = new TestWorld().CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        var baseX = DestinationRegionX * 64;
        var baseY = DestinationRegionY * 64;
        var wide = TestWorld.CreateGameObject(1, baseX + 4, baseY + 14, 0, 0, 0, ShapeType.GroundDefault, Definition(12, 1), isStatic: false);
        var near = TestWorld.CreateGameObject(2, baseX + 14, baseY + 15, 0, 0, 0, ShapeType.GroundDefault, Standard1X1, isStatic: false);
        var unrelated = TestWorld.CreateGameObject(3, baseX + 40, baseY + 40, 0, 0, 0, ShapeType.GroundDefault, Standard1X1, isStatic: false);
        var otherPlane = TestWorld.CreateGameObject(4, baseX + 14, baseY + 14, 1, 0, 0, ShapeType.GroundDefault, Standard1X1, isStatic: false);
        foreach (var gameObject in new[] { wide, near, unrelated, otherPlane })
        {
            region.Add(gameObject);
        }

        var found = region.FindGameObjectsOverlapping(baseX + 14, baseY + 14, baseX + 14, baseY + 14, 0).ToList();

        CollectionAssert.AreEquivalent(new[] { wide, near }, found, "A wide object rooted parts away is a candidate; unrelated parts and planes are not scanned.");

        // A huge object that was added and removed again widens the reach, but the search stays inside the region's own parts.
        var huge = TestWorld.CreateGameObject(5, baseX + 40, baseY + 50, 0, 0, 0, ShapeType.GroundDefault, Definition(255, 1), isStatic: false);
        region.Add(huge);
        region.Remove(huge);
        CollectionAssert.AreEquivalent(new[] { wide, near }, region.FindGameObjectsOverlapping(baseX + 14, baseY + 14, baseX + 14, baseY + 14, 0).ToList());
        Assert.AreEqual(0, region.FindGameObjectsOverlapping(baseX + 1000, baseY + 1000, baseX + 1000, baseY + 1000, 0).Count());
    }

    [TestMethod]
    public void SourceObjectIndex_ReturnsOnlyThePartsObjectsInSourceOrder()
    {
        var template = CreateSourceTemplate(new MapRegionTemplateObject(60, SourceMinX + 8, SourceMinY, SourceZ, 0, ShapeType.GroundDefault, Standard1X1, true));

        var objects = template.GetSourceObjects(SourceZ, SourcePartX & 7, SourcePartY & 7);

        CollectionAssert.AreEqual(SourceObjects.Select(obj => obj.Id).ToArray(), objects.Select(obj => obj.Id).ToArray());
        Assert.AreEqual(0, template.GetSourceObjects(0, SourcePartX & 7, SourcePartY & 7).Count);
        Assert.AreEqual(60, template.GetSourceObjects(SourceZ, (SourcePartX & 7) + 1, SourcePartY & 7).Single().Id);
    }

    // Plane 0 holds a static 1x1 object (id 70) at chunk tile (1, 1); plane 1 maps the same chunk, which has no
    // bridge. The neighbouring source chunk has a bridge bit on that tile, so mapping plane 1 to it adds a bridge.
    private static (MapRegion Destination, IReadOnlyDictionary<int, MapRegionTemplate> Templates) CreateBridgeFreeObjectRegion()
    {
        var flags = new sbyte[4, 64, 64];
        flags[1, SourceMinX + 8 + 1, SourceMinY + 1] = 0x2;
        var template = new MapRegionTemplate(SourceRegionId, [], [],
            [new MapRegionTemplateObject(70, SourceMinX + 1, SourceMinY + 1, SourceZ, 0, ShapeType.GroundDefault, Standard1X1, true)], flags);
        var templates = Templates(template);
        var destination = new TestWorld().CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlocksByIndex(
        [
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, 0),
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX, SourcePartY, SourceZ, 0)
        ], templates);
        return (destination, templates);
    }

    // A configured spawn (id 50) at chunk tile (7, 7) of source chunk (2, 3), and a static 2x1 object (id 51)
    // at chunk tile (7, 0) of source chunk (3, 3). Rotated by 2 in column (6, 6), the static roots on the
    // spawn's destination tile (47, 55). Source chunk (3, 3) has a bridge bit on plane 1 at its tile (7, 7).
    private static IReadOnlyDictionary<int, MapRegionTemplate> CreateSpawnAndRotatedStaticTemplates()
    {
        var flags = new sbyte[4, 64, 64];
        flags[1, SourceMinX + 8 + 7, SourceMinY + 7] = 0x2;
        return Templates(new MapRegionTemplate(SourceRegionId, [], [],
        [
            new MapRegionTemplateObject(50, SourceMinX + 7, SourceMinY + 7, SourceZ, 0, ShapeType.GroundDefault, Standard1X1, false),
            new MapRegionTemplateObject(51, SourceMinX + 8 + 7, SourceMinY, SourceZ, 0, ShapeType.GroundDefault, Definition(2, 1), true)
        ], flags));
    }

    // Column (5, 6) plane 0 holds the spawn, column (6, 6) plane 1 holds the rotated static, and plane 1 of
    // column (5, 6) maps the bridge chunk, so the static sits on plane 0 at the spawn's tile.
    private static (MapRegion Destination, IReadOnlyDictionary<int, MapRegionTemplate> Templates) CreateBridgeMovedStaticRegion()
    {
        var templates = CreateSpawnAndRotatedStaticTemplates();
        var destination = new TestWorld().CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlocksByIndex(
        [
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, 0),
            new MapRegionPartMapping(DestinationLocalPartX + 1, DestinationLocalPartY, 1, SourcePartX + 1, SourcePartY, SourceZ, 2),
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX + 1, SourcePartY, SourceZ, 0)
        ], templates);
        return (destination, templates);
    }

    // The same column on planes 0 and 1, both mapped from the same bridge-free source chunk.
    private static (MapRegion Destination, MapRegionTemplate Template) CreateTwoPlaneRegion()
    {
        var template = CreateSourceTemplate();
        var destination = new TestWorld().CreateDynamicRegion(DestinationRegionX, DestinationRegionY);
        destination.WriteBlocksByIndex(
        [
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, 0),
            new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 1, SourcePartX, SourcePartY, SourceZ, 0)
        ], Templates(template));
        return (destination, template);
    }

    // A: column (5, 6) maps source chunk (2, 3), whose 1x1 object at chunk (7, 7) lands on (47, 55).
    // B: column (6, 6) maps source chunk (3, 3) rotated by 2, whose 2x1 object at chunk (7, 0)
    // roots on the same slot. B loads later in the client's order, so it keeps the slot.
    private static MapRegionTemplate CreateSharedSlotTemplate() => new(SourceRegionId,
    [
        new MapRegionTemplateObject(50, SourceMinX + 7, SourceMinY + 7, SourceZ, 0, ShapeType.GroundDefault, Standard1X1, true),
        new MapRegionTemplateObject(51, SourceMinX + 8 + 7, SourceMinY, SourceZ, 0, ShapeType.GroundDefault, Definition(2, 1), true)
    ], []);

    private static MapRegionPartMapping[] SharedSlotMappings() =>
    [
        new MapRegionPartMapping(DestinationLocalPartX, DestinationLocalPartY, 0, SourcePartX, SourcePartY, SourceZ, 0),
        new MapRegionPartMapping(DestinationLocalPartX + 1, DestinationLocalPartY, 0, SourcePartX + 1, SourcePartY, SourceZ, 2)
    ];

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
        region.WriteBlockByIndex(0, 0, 0, SourcePartX, SourcePartY, SourceZ, Templates(CreateSourceTemplate(), ownTemplate));
        region.RotateBlockByIndex(DestinationLocalPartX, DestinationLocalPartY, 0, 3, Templates(CreateSourceTemplate(), ownTemplate));
        region.DeleteBlockByIndex(1, 1, 0, Templates(CreateSourceTemplate(), ownTemplate));

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

    private static MapRegionTemplate CreateTemplateWithTerrain(MapRegionTemplateObject[] sourceObjects, Action<sbyte[,,]> terrain)
    {
        var flags = new sbyte[4, 64, 64];
        terrain(flags);
        return new MapRegionTemplate(SourceRegionId, [], [], sourceObjects, flags);
    }

    private static IReadOnlyDictionary<int, MapRegionTemplate> Templates(params MapRegionTemplate[] templates) =>
        templates.ToDictionary(template => template.RegionId);

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

    // A 2x3 object at the east edge of source part (2, 3) and a 1x1 object in source part (3, 3)
    // on a tile the 2x3 footprint covers once both parts are placed side by side.
    private static MapRegionTemplate CreateOverlapTemplate() => new(SourceRegionId,
    [
        new MapRegionTemplateObject(10, SourceMinX + 7, SourceMinY, SourceZ, 0, ShapeType.GroundDefault, Standard2X3, true),
        new MapRegionTemplateObject(11, SourceMinX + 8, SourceMinY + 1, SourceZ, 0, ShapeType.GroundDefault, Standard1X1, true)
    ], []);

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
            _regionService.FindMapRegion(Arg.Any<int>(), Arg.Any<int>())
                .Returns(call => _regions.GetValueOrDefault((call.ArgAt<int>(0), call.ArgAt<int>(1))));
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

        public static GameObject CreateGameObject(int id, int x, int y, int z, int dimension, int rotation, ShapeType shape, IGameObjectDefinition definition, bool isStatic = true) =>
            new(id, Location.Create(x, y, z, dimension), rotation, shape, isStatic, definition, Substitute.For<IGameObjectScript>());

        private void Route(ILocation location, CollisionFlag flag, Action<MapRegion, int, int, int, CollisionFlag> action)
        {
            if (_regions.TryGetValue((location.RegionId, location.Dimension), out var region))
            {
                action(region, location.RegionLocalX, location.RegionLocalY, location.Z, flag);
            }
        }
    }
}
