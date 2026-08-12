using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Extensions;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class ProjectilePathfinderTests
{
    private const int OriginX = 50;
    private const int OriginY = 50;
    private const int Plane = 0;
    private const int Dimension = 0;

    public static IEnumerable<TestDataRow<CollisionFlag>> DirectBlockers =>
    [
        new(CollisionFlag.FloorBlock) { DisplayName = "floor" },
        new(CollisionFlag.FloorDecorationBlock) { DisplayName = "floor decoration" },
        new(CollisionFlag.FloorBlock | CollisionFlag.ObjectBlock | CollisionFlag.ObjectAllowRange)
        {
            DisplayName = "floor and range-permissive object"
        },
        new(CollisionFlag.FloorDecorationBlock | CollisionFlag.ObjectBlock | CollisionFlag.ObjectAllowRange)
        {
            DisplayName = "floor decoration and range-permissive object"
        }
    ];

    public static IEnumerable<TestDataRow<StandardObjectCase>> StandardObjectCases =>
    [
        new(new StandardObjectCase(
            "range-permissive",
            Solid: true,
            Gateway: false,
            ExpectedCollision: CollisionFlag.ObjectBlock | CollisionFlag.ObjectAllowRange,
            ExpectedSuccessful: true)),
        new(new StandardObjectCase(
            "high-layer-only",
            Solid: false,
            Gateway: false,
            ExpectedCollision: CollisionFlag.ObjectAllowRange,
            ExpectedSuccessful: false)),
        new(new StandardObjectCase(
            "gateway",
            Solid: true,
            Gateway: true,
            ExpectedCollision: CollisionFlag.ObjectBlock,
            ExpectedSuccessful: true))
    ];

    public static IEnumerable<TestDataRow<WallTraversalCase>> CardinalWallCases =>
    [
        Case(
            "rotation 0 east and west",
            0,
            new(OriginX - 1, OriginY, OriginX, OriginY, CollisionFlag.WallAllowRangeWest),
            new(OriginX, OriginY, OriginX - 1, OriginY, CollisionFlag.WallAllowRangeEast)),
        Case(
            "rotation 1 north and south",
            1,
            new(OriginX, OriginY, OriginX, OriginY + 1, CollisionFlag.WallAllowRangeSouth),
            new(OriginX, OriginY + 1, OriginX, OriginY, CollisionFlag.WallAllowRangeNorth)),
        Case(
            "rotation 2 east and west",
            2,
            new(OriginX, OriginY, OriginX + 1, OriginY, CollisionFlag.WallAllowRangeWest),
            new(OriginX + 1, OriginY, OriginX, OriginY, CollisionFlag.WallAllowRangeEast)),
        Case(
            "rotation 3 north and south",
            3,
            new(OriginX, OriginY - 1, OriginX, OriginY, CollisionFlag.WallAllowRangeSouth),
            new(OriginX, OriginY, OriginX, OriginY - 1, CollisionFlag.WallAllowRangeNorth))
    ];

    public static IEnumerable<TestDataRow<DiagonalWallCase>> DiagonalWallCases =>
    [
        new(new DiagonalWallCase("north-west", 0, OriginX + 1, OriginY - 1, CollisionFlag.WallAllowRangeNorthWest)),
        new(new DiagonalWallCase("north-east", 1, OriginX - 1, OriginY - 1, CollisionFlag.WallAllowRangeNorthEast)),
        new(new DiagonalWallCase("south-east", 2, OriginX - 1, OriginY + 1, CollisionFlag.WallAllowRangeSouthEast)),
        new(new DiagonalWallCase("south-west", 3, OriginX + 1, OriginY + 1, CollisionFlag.WallAllowRangeSouthWest))
    ];

    [TestMethod]
    public void Find_EmptyTiles_ReturnsSuccessfulPath()
    {
        var destination = Location.Create(OriginX + 2, OriginY, Plane, Dimension);

        var path = Find(new Dictionary<(int X, int Y), CollisionFlag>(), Location.Create(OriginX, OriginY, Plane, Dimension), destination);

        Assert.IsTrue(path.Successful);
        Assert.AreEqual(destination, path.Last());
    }

    [TestMethod]
    [DynamicData(nameof(DirectBlockers))]
    public void Find_DirectProjectileBlocker_ReturnsUnsuccessfulPath(CollisionFlag blocker)
    {
        var destination = Location.Create(OriginX + 1, OriginY, Plane, Dimension);
        var collision = new Dictionary<(int X, int Y), CollisionFlag>
        {
            [(destination.X, destination.Y)] = blocker
        };

        var path = Find(collision, Location.Create(OriginX, OriginY, Plane, Dimension), destination);

        Assert.IsFalse(path.Successful);
    }

    [TestMethod]
    [DynamicData(nameof(StandardObjectCases))]
    public void Find_StandardObjectWriter_UsesItsProjectileLayer(StandardObjectCase testCase)
    {
        var collision = WriteStandardObject(testCase.Solid, testCase.Gateway);
        var destination = Location.Create(OriginX, OriginY, Plane, Dimension);
        var emittedFlags = collision[(destination.X, destination.Y)];

        Assert.AreEqual(
            testCase.ExpectedCollision,
            emittedFlags & (CollisionFlag.ObjectBlock | CollisionFlag.ObjectAllowRange));

        var path = Find(collision, Location.Create(OriginX - 1, OriginY, Plane, Dimension), destination);

        Assert.AreEqual(testCase.ExpectedSuccessful, path.Successful);
    }

    [TestMethod]
    [DynamicData(nameof(CardinalWallCases))]
    public void Find_CardinalWallWriter_BlocksProjectileTraversalFromBothSides(WallTraversalCase testCase)
    {
        var collision = WriteWall(ShapeType.Wall, testCase.Rotation);

        foreach (var route in testCase.Routes)
        {
            Assert.IsTrue((collision[(route.ToX, route.ToY)] & route.ExpectedBlocker) != 0);

            var path = Find(
                collision,
                Location.Create(route.FromX, route.FromY, Plane, Dimension),
                Location.Create(route.ToX, route.ToY, Plane, Dimension));

            Assert.IsFalse(path.Successful, $"{testCase.Name} should block the route to ({route.ToX}, {route.ToY}).");
        }
    }

    [TestMethod]
    [DynamicData(nameof(DiagonalWallCases))]
    public void Find_DiagonalWallWriter_BlocksProjectileTraversal(DiagonalWallCase testCase)
    {
        var collision = WriteWall(ShapeType.WallCorner, testCase.Rotation);
        var destination = Location.Create(OriginX, OriginY, Plane, Dimension);

        Assert.IsTrue((collision[(destination.X, destination.Y)] & testCase.ExpectedBlocker) != 0);

        var path = Find(collision, Location.Create(testCase.FromX, testCase.FromY, Plane, Dimension), destination);

        Assert.IsFalse(path.Successful);
    }

    [TestMethod]
    public void Find_ClearSouthWestStep_EndsAtTheSouthWestTile()
    {
        var from = Location.Create(OriginX, OriginY, Plane, Dimension);
        var destination = Location.Create(OriginX - 1, OriginY - 1, Plane, Dimension);

        var path = Find(new Dictionary<(int X, int Y), CollisionFlag>(), from, destination);

        Assert.IsTrue(path.Successful);
        Assert.HasCount(1, path);
        Assert.AreEqual(destination, path.Single());
    }

    private static TestDataRow<WallTraversalCase> Case(string name, int rotation, params Route[] routes) =>
        new(new WallTraversalCase(name, rotation, routes)) { DisplayName = name };

    private static IPath Find(
        IReadOnlyDictionary<(int X, int Y), CollisionFlag> collision,
        Location from,
        Location destination)
    {
        var mapRegionService = Substitute.For<IMapRegionService>();
        mapRegionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(callInfo => collision.GetValueOrDefault((callInfo.ArgAt<int>(0), callInfo.ArgAt<int>(1)), CollisionFlag.Walkable));

        return new ProjectilePathFinder(mapRegionService).Find(from, 1, destination, 1, 1, 0, 0, 0, false);
    }

    private static Dictionary<(int X, int Y), CollisionFlag> WriteStandardObject(bool solid, bool gateway) =>
        WriteCollision(ShapeType.GroundDefault, 0, solid, gateway);

    private static Dictionary<(int X, int Y), CollisionFlag> WriteWall(ShapeType shapeType, int rotation) =>
        WriteCollision(shapeType, rotation, solid: true, gateway: false);

    private static Dictionary<(int X, int Y), CollisionFlag> WriteCollision(
        ShapeType shapeType,
        int rotation,
        bool solid,
        bool gateway)
    {
        var mapRegionService = Substitute.For<IMapRegionService>();
        var collision = new Dictionary<(int X, int Y), CollisionFlag>();
        mapRegionService.When(service => service.FlagCollision(Arg.Any<ILocation>(), Arg.Any<CollisionFlag>()))
            .Do(callInfo =>
            {
                var location = callInfo.Arg<ILocation>()!;
                var coordinate = (location.X, location.Y);
                collision[coordinate] = collision.GetValueOrDefault(coordinate, CollisionFlag.Walkable) | callInfo.Arg<CollisionFlag>();
            });

        var region = new MapRegion(
            Location.Zero,
            new int[4],
            Substitute.For<INpcService>(),
            mapRegionService,
            Substitute.For<IGameObjectBuilder>(),
            Substitute.For<IGroundItemBuilder>(),
            Substitute.For<IMapper>());
        var gameObject = CreateGameObject(shapeType, rotation, solid, gateway);

        if (shapeType.GetLayerType() == LayerType.StandardObjects)
        {
            region.FlagStandardObject(gameObject);
        }
        else
        {
            region.FlagWallObject(gameObject);
        }

        return collision;
    }

    private static IGameObject CreateGameObject(ShapeType shapeType, int rotation, bool solid, bool gateway)
    {
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.ClipType.Returns(1);
        definition.Solid.Returns(solid);
        definition.Gateway.Returns(gateway);
        definition.SizeX.Returns(1);
        definition.SizeY.Returns(1);

        var gameObject = Substitute.For<IGameObject>();
        gameObject.ShapeType.Returns(shapeType);
        gameObject.Rotation.Returns(rotation);
        gameObject.Location.Returns(Location.Create(OriginX, OriginY, Plane, Dimension));
        gameObject.Definition.Returns(definition);
        return gameObject;
    }

    public sealed record StandardObjectCase(
        string Name,
        bool Solid,
        bool Gateway,
        CollisionFlag ExpectedCollision,
        bool ExpectedSuccessful);

    public sealed record WallTraversalCase(string Name, int Rotation, IReadOnlyList<Route> Routes);

    public sealed record DiagonalWallCase(string Name, int Rotation, int FromX, int FromY, CollisionFlag ExpectedBlocker);

    public readonly record struct Route(int FromX, int FromY, int ToX, int ToY, CollisionFlag ExpectedBlocker);
}
