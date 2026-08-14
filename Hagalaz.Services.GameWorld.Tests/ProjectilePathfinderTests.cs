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
using Path = Hagalaz.Services.GameWorld.Logic.Pathfinding.Path;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class ProjectilePathfinderTests
{
    private const int QueueSize = 4096;
    private const int OriginX = 50;
    private const int OriginY = 50;
    private const int Plane = 0;
    private const int Dimension = 0;
    private static readonly IReadOnlyDictionary<(int X, int Y), CollisionFlag> EmptyCollision = new Dictionary<(int X, int Y), CollisionFlag>();

    public static IEnumerable<TestDataRow<StandardObjectCase>> StandardObjectCases =>
    [
        new(new StandardObjectCase(
            "solid non-gateway",
            Solid: true,
            Gateway: false,
            ExpectedCollision: CollisionFlag.ObjectBlock | CollisionFlag.ObjectAllowRange,
            ExpectedSuccessful: false)),
        new(new StandardObjectCase(
            "solid gateway",
            Solid: true,
            Gateway: true,
            ExpectedCollision: CollisionFlag.ObjectBlock,
            ExpectedSuccessful: false)),
        new(new StandardObjectCase(
            "non-solid non-gateway",
            Solid: false,
            Gateway: false,
            ExpectedCollision: CollisionFlag.ObjectAllowRange,
            ExpectedSuccessful: true)),
        new(new StandardObjectCase(
            "non-solid gateway",
            Solid: false,
            Gateway: true,
            ExpectedCollision: CollisionFlag.Walkable,
            ExpectedSuccessful: true))
    ];

    public static IEnumerable<TestDataRow<GatewayWallCase>> CardinalGatewayWallCases =>
    [
        WallCase(
            "rotation 0 east and west",
            0,
            new(OriginX - 1, OriginY, OriginX, OriginY, CollisionFlag.BlockedWest),
            new(OriginX, OriginY, OriginX - 1, OriginY, CollisionFlag.BlockedEast)),
        WallCase(
            "rotation 1 north and south",
            1,
            new(OriginX, OriginY, OriginX, OriginY + 1, CollisionFlag.BlockedSouth),
            new(OriginX, OriginY + 1, OriginX, OriginY, CollisionFlag.BlockedNorth)),
        WallCase(
            "rotation 2 east and west",
            2,
            new(OriginX, OriginY, OriginX + 1, OriginY, CollisionFlag.BlockedWest),
            new(OriginX + 1, OriginY, OriginX, OriginY, CollisionFlag.BlockedEast)),
        WallCase(
            "rotation 3 north and south",
            3,
            new(OriginX, OriginY - 1, OriginX, OriginY, CollisionFlag.BlockedSouth),
            new(OriginX, OriginY, OriginX, OriginY - 1, CollisionFlag.BlockedNorth))
    ];

    public static IEnumerable<TestDataRow<AsymmetricRayCase>> AsymmetricRays =>
    [
        new(new AsymmetricRayCase(
            "north-east",
            5,
            2,
            [new(1, 0), new(2, 0), new(2, 1), new(3, 1), new(4, 1), new(4, 2), new(5, 2)])),
        new(new AsymmetricRayCase(
            "south-west",
            -5,
            -2,
            [new(-1, 0), new(-2, 0), new(-2, -1), new(-3, -1), new(-4, -1), new(-4, -2), new(-5, -2)])),
        new(new AsymmetricRayCase(
            "steep north-east",
            2,
            5,
            [new(0, 1), new(0, 2), new(1, 2), new(1, 3), new(1, 4), new(2, 4), new(2, 5)])),
        new(new AsymmetricRayCase(
            "steep south-west",
            -2,
            -5,
            [new(0, -1), new(0, -2), new(-1, -2), new(-1, -3), new(-1, -4), new(-2, -4), new(-2, -5)]))
    ];

    public static IEnumerable<TestDataRow<(int DeltaX, int DeltaY)>> SlopeQuadrants =>
    [
        new((5, 2)), new((5, -2)), new((-5, 2)), new((-5, -2)),
        new((2, 5)), new((2, -5)), new((-2, 5)), new((-2, -5))
    ];

    public static IEnumerable<TestDataRow<RayBlockerCase>> AsymmetricRayBlockers =>
    [
        new(new RayBlockerCase("x boundary", 1, 0, CollisionFlag.BlockedWest)),
        new(new RayBlockerCase("y boundary", 2, 1, CollisionFlag.BlockedSouth))
    ];

    public static IEnumerable<TestDataRow<DiagonalWallCase>> DiagonalWallCases =>
    [
        new(new DiagonalWallCase("rotation 0 NW neighbor to origin", 0, -1, 1, 0, 0, CollisionFlag.BlockedNorthWest)),
        new(new DiagonalWallCase("rotation 0 origin to NW neighbor", 0, 0, 0, -1, 1, CollisionFlag.BlockedSouthEast)),
        new(new DiagonalWallCase("rotation 1 NE neighbor to origin", 1, 1, 1, 0, 0, CollisionFlag.BlockedNorthEast)),
        new(new DiagonalWallCase("rotation 1 origin to NE neighbor", 1, 0, 0, 1, 1, CollisionFlag.BlockedSouthWest)),
        new(new DiagonalWallCase("rotation 2 SE neighbor to origin", 2, 1, -1, 0, 0, CollisionFlag.BlockedSouthEast)),
        new(new DiagonalWallCase("rotation 2 origin to SE neighbor", 2, 0, 0, 1, -1, CollisionFlag.BlockedNorthWest)),
        new(new DiagonalWallCase("rotation 3 SW neighbor to origin", 3, -1, -1, 0, 0, CollisionFlag.BlockedSouthWest)),
        new(new DiagonalWallCase("rotation 3 origin to SW neighbor", 3, 0, 0, -1, -1, CollisionFlag.BlockedNorthEast))
    ];

    [TestMethod]
    public void Find_EmptyTiles_ReturnsSuccessfulPath()
    {
        var destination = Location.Create(OriginX + 2, OriginY, Plane, Dimension);

        var path = Find(EmptyCollision, Location.Create(OriginX, OriginY, Plane, Dimension), destination);

        Assert.IsTrue(path.Successful);
        Assert.AreEqual(destination, path.Last());
    }

    [TestMethod]
    public void Find_SingleStepPath_SetsDestinationFlags()
    {
        var destination = Location.Create(OriginX + 1, OriginY, Plane, Dimension);

        var path = (Path)Find(EmptyCollision, Location.Create(OriginX, OriginY, Plane, Dimension), destination);

        Assert.IsTrue(path.Successful);
        Assert.AreEqual(0, path.Steps);
        Assert.IsTrue(path.ReachedDestination);
        Assert.IsFalse(path.MovedNearDestination);
    }

    [TestMethod]
    [DataRow(QueueSize + 1, 0)]
    [DataRow(0, QueueSize + 1)]
    public void Find_PathExceedsStepLimit_ReturnsUnsuccessfulTrace(int deltaX, int deltaY)
    {
        var from = Location.Create(OriginX, OriginY, Plane, Dimension);
        var destination = Location.Create(OriginX + deltaX, OriginY + deltaY, Plane, Dimension);

        var path = (Path)Find(EmptyCollision, from, destination);

        Assert.IsFalse(path.Successful);
        Assert.AreEqual(0, path.Count());
        Assert.AreEqual(QueueSize, path.Steps);
        Assert.IsTrue(path.MovedNear);
        Assert.IsFalse(path.ReachedDestination);
        Assert.IsFalse(path.MovedNearDestination);
    }

    [TestMethod]
    [DataRow(QueueSize, 0)]
    [DataRow(0, QueueSize)]
    public void Find_PathReachingFinalPermittedStep_RemainsSuccessful(int deltaX, int deltaY)
    {
        var from = Location.Create(OriginX, OriginY, Plane, Dimension);
        var destination = Location.Create(OriginX + deltaX, OriginY + deltaY, Plane, Dimension);

        var path = (Path)Find(EmptyCollision, from, destination);

        Assert.IsTrue(path.Successful);
        Assert.HasCount(1, path);
        Assert.AreEqual(destination, path.Single());
        Assert.AreEqual(QueueSize - 1, path.Steps);
        Assert.IsFalse(path.MovedNear);
        Assert.IsFalse(path.ReachedDestination);
        Assert.IsFalse(path.MovedNearDestination);
    }

    [TestMethod]
    public void Find_PathBlockedBeforeStepLimit_PreservesCollisionFailureState()
    {
        var from = Location.Create(OriginX, OriginY, Plane, Dimension);
        var destination = Location.Create(OriginX + 3, OriginY, Plane, Dimension);
        var collision = new Dictionary<(int X, int Y), CollisionFlag>
        {
            [(OriginX + 2, OriginY)] = CollisionFlag.ObjectBlock
        };

        var path = (Path)Find(collision, from, destination);

        Assert.IsFalse(path.Successful);
        Assert.AreEqual(0, path.Count());
        Assert.AreEqual(1, path.Steps);
        Assert.IsTrue(path.MovedNear);
        Assert.IsFalse(path.ReachedDestination);
        Assert.IsFalse(path.MovedNearDestination);
    }

    [TestMethod]
    [DynamicData(nameof(StandardObjectCases))]
    public void Find_StandardObjectWriter_UsesTheLineOfSightLayer(StandardObjectCase testCase)
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
    [DynamicData(nameof(CardinalGatewayWallCases))]
    public void Find_GatewayWallWriter_BlocksLineOfSightFromBothSides(GatewayWallCase testCase)
    {
        var collision = WriteWall(ShapeType.Wall, testCase.Rotation, solid: true, gateway: true);

        foreach (var route in testCase.Routes)
        {
            var emittedFlags = collision[(route.ToX, route.ToY)];
            Assert.IsTrue((emittedFlags & route.ExpectedBlocker) != 0);
            Assert.IsFalse((emittedFlags & RangeLayerMask) != 0);

            var path = Find(
                collision,
                Location.Create(route.FromX, route.FromY, Plane, Dimension),
                Location.Create(route.ToX, route.ToY, Plane, Dimension));

            Assert.IsFalse(path.Successful, $"{testCase.Name} should block the route to ({route.ToX}, {route.ToY}).");
        }
    }

    [TestMethod]
    public void Find_HighRoutingWallWithoutMiddleLineOfSightLayer_ReturnsSuccessfulPath()
    {
        var collision = WriteWall(ShapeType.Wall, rotation: 0, solid: false, gateway: false);
        var destination = Location.Create(OriginX, OriginY, Plane, Dimension);
        var emittedFlags = collision[(destination.X, destination.Y)];

        Assert.IsTrue((emittedFlags & CollisionFlag.WallAllowRangeWest) != 0);
        Assert.IsFalse((emittedFlags & CollisionFlag.BlockedWest) != 0);

        var path = Find(collision, Location.Create(OriginX - 1, OriginY, Plane, Dimension), destination);

        Assert.IsTrue(path.Successful);
    }

    [TestMethod]
    [DynamicData(nameof(DiagonalWallCases))]
    public void Find_DiagonalWallWriter_BlocksLineOfSightInCorrespondingGeometry(DiagonalWallCase testCase)
    {
        var collision = WriteWall(ShapeType.WallCorner, testCase.Rotation, solid: true, gateway: true);
        var destination = Location.Create(OriginX + testCase.ToOffsetX, OriginY + testCase.ToOffsetY, Plane, Dimension);
        var emittedFlags = collision[(destination.X, destination.Y)];

        Assert.IsTrue((emittedFlags & testCase.ExpectedBlocker) != 0);
        Assert.IsFalse((emittedFlags & RangeLayerMask) != 0);

        var path = Find(
            collision,
            Location.Create(OriginX + testCase.FromOffsetX, OriginY + testCase.FromOffsetY, Plane, Dimension),
            destination);

        Assert.IsFalse(path.Successful);
    }

    [TestMethod]
    [DataRow(CollisionFlag.FloorBlock)]
    [DataRow(CollisionFlag.FloorDecorationBlock)]
    public void Find_MovementOnlyCollision_ReturnsSuccessfulPath(CollisionFlag collisionFlag)
    {
        var destination = Location.Create(OriginX + 1, OriginY, Plane, Dimension);
        var collision = new Dictionary<(int X, int Y), CollisionFlag>
        {
            [(destination.X, destination.Y)] = collisionFlag
        };

        var path = Find(collision, Location.Create(OriginX, OriginY, Plane, Dimension), destination);

        Assert.IsTrue(path.Successful);
    }

    [TestMethod]
    [DataRow(CollisionFlag.ObjectBlock | CollisionFlag.FloorBlock)]
    [DataRow(CollisionFlag.BlockedWest | CollisionFlag.FloorDecorationBlock)]
    public void Find_LineOfSightBlockerWithMovementFlags_ReturnsUnsuccessfulPath(CollisionFlag collisionFlag)
    {
        var destination = Location.Create(OriginX + 1, OriginY, Plane, Dimension);
        var collision = new Dictionary<(int X, int Y), CollisionFlag>
        {
            [(destination.X, destination.Y)] = collisionFlag
        };

        var path = Find(collision, Location.Create(OriginX, OriginY, Plane, Dimension), destination);

        Assert.IsFalse(path.Successful);
    }

    [TestMethod]
    [DynamicData(nameof(AsymmetricRays))]
    public void Find_AsymmetricRay_FollowsFixedPointCrossings(AsymmetricRayCase testCase)
    {
        var from = Location.Create(OriginX, OriginY, Plane, Dimension);
        var destination = Location.Create(OriginX + testCase.DeltaX, OriginY + testCase.DeltaY, Plane, Dimension);
        var expectedTrace = testCase.Crossings
            .Select(crossing => Location.Create(OriginX + crossing.X, OriginY + crossing.Y, Plane, Dimension))
            .ToArray();

        var (path, inspectedTiles) = FindWithInspectedTiles(EmptyCollision, from, destination);

        Assert.IsTrue(path.Successful);
        CollectionAssert.AreEqual(expectedTrace, inspectedTiles.ToArray());
    }

    [TestMethod]
    [DynamicData(nameof(SlopeQuadrants))]
    public void Find_AsymmetricRayAcrossEveryQuadrant_ReturnsSuccessfulPath((int DeltaX, int DeltaY) slope)
    {
        var destination = Location.Create(OriginX + slope.DeltaX, OriginY + slope.DeltaY, Plane, Dimension);

        var path = Find(EmptyCollision, Location.Create(OriginX, OriginY, Plane, Dimension), destination);

        Assert.IsTrue(path.Successful);
    }

    [TestMethod]
    [DynamicData(nameof(AsymmetricRayBlockers))]
    public void Find_AsymmetricRay_CrossedLineOfSightBoundaryReturnsUnsuccessfulPath(RayBlockerCase testCase)
    {
        var collision = new Dictionary<(int X, int Y), CollisionFlag>
        {
            [(OriginX + testCase.XOffset, OriginY + testCase.YOffset)] = testCase.Blocker
        };
        var destination = Location.Create(OriginX + 5, OriginY + 2, Plane, Dimension);

        var path = Find(collision, Location.Create(OriginX, OriginY, Plane, Dimension), destination);

        Assert.IsFalse(path.Successful);
    }

    [TestMethod]
    public void Find_DifferentPlanes_ReturnsUnsuccessfulPath()
    {
        var path = Find(
            EmptyCollision,
            Location.Create(OriginX, OriginY, Plane, Dimension),
            Location.Create(OriginX + 1, OriginY, Plane + 1, Dimension));

        Assert.IsFalse(path.Successful);
    }

    [TestMethod]
    [DataRow(2, 0)]
    [DataRow(0, 2)]
    [DataRow(2, 2)]
    [DataRow(-2, -2)]
    public void Find_AxisAlignedAndFortyFiveDegreeRay_ReturnsSuccessfulPath(int deltaX, int deltaY)
    {
        var destination = Location.Create(OriginX + deltaX, OriginY + deltaY, Plane, Dimension);

        var path = Find(EmptyCollision, Location.Create(OriginX, OriginY, Plane, Dimension), destination);

        Assert.IsTrue(path.Successful);
    }

    [TestMethod]
    public void Find_OldDiagonalStaircaseOnlyObstacle_ReturnsSuccessfulPath()
    {
        var collision = new Dictionary<(int X, int Y), CollisionFlag>
        {
            [(OriginX + 1, OriginY + 1)] = CollisionFlag.ObjectBlock
        };
        var destination = Location.Create(OriginX + 5, OriginY + 2, Plane, Dimension);

        var path = Find(collision, Location.Create(OriginX, OriginY, Plane, Dimension), destination);

        Assert.IsTrue(path.Successful);
    }

    [TestMethod]
    public void Find_ClearSouthWestRay_AppendsOnlyTheSouthWestTile()
    {
        var destination = Location.Create(OriginX - 1, OriginY - 1, Plane, Dimension);

        var path = Find(EmptyCollision, Location.Create(OriginX, OriginY, Plane, Dimension), destination);

        Assert.IsTrue(path.Successful);
        Assert.HasCount(1, path);
        Assert.AreEqual(destination, path.Single());
    }

    private static TestDataRow<GatewayWallCase> WallCase(string name, int rotation, params Route[] routes) =>
        new(new GatewayWallCase(name, rotation, routes)) { DisplayName = name };

    private static IPath Find(
        IReadOnlyDictionary<(int X, int Y), CollisionFlag> collision,
        Location from,
        Location destination) => FindWithInspectedTiles(collision, from, destination).Path;

    private static (IPath Path, IReadOnlyList<Location> InspectedTiles) FindWithInspectedTiles(
        IReadOnlyDictionary<(int X, int Y), CollisionFlag> collision,
        Location from,
        Location destination)
    {
        var inspectedTiles = new List<Location>();
        var mapRegionService = Substitute.For<IMapRegionService>();
        mapRegionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(callInfo =>
            {
                var x = callInfo.ArgAt<int>(0);
                var y = callInfo.ArgAt<int>(1);
                var z = callInfo.ArgAt<int>(2);
                inspectedTiles.Add(Location.Create(x, y, z, Dimension));
                return collision.GetValueOrDefault((x, y), CollisionFlag.Walkable);
            });

        var path = new ProjectilePathFinder(mapRegionService).Find(from, 1, destination, 1, 1, 0, 0, 0, false);
        return (path, inspectedTiles);
    }

    private static Dictionary<(int X, int Y), CollisionFlag> WriteStandardObject(bool solid, bool gateway) =>
        WriteCollision(ShapeType.GroundDefault, 0, solid, gateway);

    private static Dictionary<(int X, int Y), CollisionFlag> WriteWall(ShapeType shapeType, int rotation, bool solid, bool gateway) =>
        WriteCollision(shapeType, rotation, solid, gateway);

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

    private const CollisionFlag RangeLayerMask =
        CollisionFlag.WallAllowRangeNorthWest | CollisionFlag.WallAllowRangeNorth | CollisionFlag.WallAllowRangeNorthEast |
        CollisionFlag.WallAllowRangeEast | CollisionFlag.WallAllowRangeSouthEast | CollisionFlag.WallAllowRangeSouth |
        CollisionFlag.WallAllowRangeSouthWest | CollisionFlag.WallAllowRangeWest;

    public sealed record StandardObjectCase(
        string Name,
        bool Solid,
        bool Gateway,
        CollisionFlag ExpectedCollision,
        bool ExpectedSuccessful);

    public sealed record GatewayWallCase(string Name, int Rotation, IReadOnlyList<Route> Routes);

    public sealed record AsymmetricRayCase(string Name, int DeltaX, int DeltaY, IReadOnlyList<(int X, int Y)> Crossings);

    public sealed record RayBlockerCase(string Name, int XOffset, int YOffset, CollisionFlag Blocker);

    public sealed record DiagonalWallCase(
        string Name,
        int Rotation,
        int FromOffsetX,
        int FromOffsetY,
        int ToOffsetX,
        int ToOffsetY,
        CollisionFlag ExpectedBlocker);

    public readonly record struct Route(int FromX, int FromY, int ToX, int ToY, CollisionFlag ExpectedBlocker);
}
