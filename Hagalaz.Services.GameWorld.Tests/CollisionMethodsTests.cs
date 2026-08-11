using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Maps.Regions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CollisionMethodsTests
{
    private const int OriginX = 10;
    private const int OriginY = 10;
    private const int Plane = 0;
    private const int Dimension = 0;
    private const CollisionFlag UnrelatedFlag = CollisionFlag.FloorBlock;

    private const CollisionFlag WallLayerMask =
        CollisionFlag.WallNorthWest | CollisionFlag.WallNorth | CollisionFlag.WallNorthEast | CollisionFlag.WallEast |
        CollisionFlag.WallSouthEast | CollisionFlag.WallSouth | CollisionFlag.WallSouthWest | CollisionFlag.WallWest;

    private const CollisionFlag BlockedLayerMask =
        CollisionFlag.BlockedNorthWest | CollisionFlag.BlockedNorth | CollisionFlag.BlockedNorthEast | CollisionFlag.BlockedEast |
        CollisionFlag.BlockedSouthEast | CollisionFlag.BlockedSouth | CollisionFlag.BlockedSouthWest | CollisionFlag.BlockedWest;

    private const CollisionFlag RangeLayerMask =
        CollisionFlag.WallAllowRangeNorthWest | CollisionFlag.WallAllowRangeNorth | CollisionFlag.WallAllowRangeNorthEast |
        CollisionFlag.WallAllowRangeEast | CollisionFlag.WallAllowRangeSouthEast | CollisionFlag.WallAllowRangeSouth |
        CollisionFlag.WallAllowRangeSouthWest | CollisionFlag.WallAllowRangeWest;

    public static IEnumerable<TestDataRow<WallCase>> WallCases =>
    [
        WallCaseFor(ShapeType.Wall, 0, [new(0, 0, CollisionFlag.WallWest), new(-1, 0, CollisionFlag.WallEast)]),
        WallCaseFor(ShapeType.Wall, 1, [new(0, 0, CollisionFlag.WallNorth), new(0, 1, CollisionFlag.WallSouth)]),
        WallCaseFor(ShapeType.Wall, 2, [new(0, 0, CollisionFlag.WallEast), new(1, 0, CollisionFlag.WallWest)]),
        WallCaseFor(ShapeType.Wall, 3, [new(0, 0, CollisionFlag.WallSouth), new(0, -1, CollisionFlag.WallNorth)]),

        WallCaseFor(ShapeType.WallCornerDiagonal, 0, [new(0, 0, CollisionFlag.WallNorthWest), new(-1, 1, CollisionFlag.WallSouthEast)]),
        WallCaseFor(ShapeType.WallCornerDiagonal, 1, [new(0, 0, CollisionFlag.WallNorthEast), new(1, 1, CollisionFlag.WallSouthWest)]),
        WallCaseFor(ShapeType.WallCornerDiagonal, 2, [new(0, 0, CollisionFlag.WallSouthEast), new(1, -1, CollisionFlag.WallNorthWest)]),
        WallCaseFor(ShapeType.WallCornerDiagonal, 3, [new(0, 0, CollisionFlag.WallSouthWest), new(-1, -1, CollisionFlag.WallNorthEast)]),

        WallCaseFor(ShapeType.WallCorner, 0, [new(0, 0, CollisionFlag.WallNorthWest), new(-1, 1, CollisionFlag.WallSouthEast)]),
        WallCaseFor(ShapeType.WallCorner, 1, [new(0, 0, CollisionFlag.WallNorthEast), new(1, 1, CollisionFlag.WallSouthWest)]),
        WallCaseFor(ShapeType.WallCorner, 2, [new(0, 0, CollisionFlag.WallSouthEast), new(1, -1, CollisionFlag.WallNorthWest)]),
        WallCaseFor(ShapeType.WallCorner, 3, [new(0, 0, CollisionFlag.WallSouthWest), new(-1, -1, CollisionFlag.WallNorthEast)]),

        WallCaseFor(ShapeType.UnfinishedWall, 0,
        [
            new(0, 0, CollisionFlag.WallWest | CollisionFlag.WallNorth),
            new(-1, 0, CollisionFlag.WallEast),
            new(0, 1, CollisionFlag.WallSouth)
        ]),
        WallCaseFor(ShapeType.UnfinishedWall, 1,
        [
            new(0, 0, CollisionFlag.WallNorth | CollisionFlag.WallEast),
            new(0, 1, CollisionFlag.WallSouth),
            new(1, 0, CollisionFlag.WallWest)
        ]),
        WallCaseFor(ShapeType.UnfinishedWall, 2,
        [
            new(0, 0, CollisionFlag.WallSouth | CollisionFlag.WallEast),
            new(1, 0, CollisionFlag.WallWest),
            new(0, -1, CollisionFlag.WallNorth)
        ]),
        WallCaseFor(ShapeType.UnfinishedWall, 3,
        [
            new(0, 0, CollisionFlag.WallWest | CollisionFlag.WallSouth),
            new(0, -1, CollisionFlag.WallNorth),
            new(-1, 0, CollisionFlag.WallEast)
        ])
    ];

    public static IEnumerable<TestDataRow<StandardCase>> StandardCases =>
    [
        StandardCaseFor(0, true, false),
        StandardCaseFor(1, true, false),
        StandardCaseFor(2, true, false),
        StandardCaseFor(3, true, false),
        StandardCaseFor(0, false, false),
        StandardCaseFor(0, true, true),
        StandardCaseFor(0, false, true)
    ];

    [TestMethod]
    [DynamicData(nameof(WallCases))]
    public void FlagWallObject_AllShapesAndRotations_WritesExpectedLayers(WallCase testCase)
    {
        // Arrange
        var (region, recorder) = CreateRegion();
        var gameObject = CreateGameObject(testCase.ShapeType, testCase.Rotation);
        SeedWallTiles(recorder, testCase.Tiles);

        // Act
        region.FlagWallObject(gameObject);

        // Assert
        AssertWallState(recorder, testCase.Tiles, solid: true, gateway: false);
    }

    [TestMethod]
    [DynamicData(nameof(WallCases))]
    public void WallObject_AllShapesAndRotations_FlagThenUnflag_RestoresUnrelatedCollision(WallCase testCase)
    {
        // Arrange
        var (region, recorder) = CreateRegion();
        var gameObject = CreateGameObject(testCase.ShapeType, testCase.Rotation);
        SeedWallTiles(recorder, testCase.Tiles);

        // Act
        region.FlagWallObject(gameObject);
        region.UnFlagWallObject(gameObject);

        // Assert
        AssertWallBaseline(recorder, testCase.Tiles);
    }

    [TestMethod]
    public void FlagWallObject_NonSolid_OmitsOnlyBlockedLayer()
    {
        // Arrange
        var (region, recorder) = CreateRegion();
        var gameObject = CreateGameObject(ShapeType.Wall, 0, solid: false);
        ExpectedTile[] expectedTiles = [new(0, 0, CollisionFlag.WallWest), new(-1, 0, CollisionFlag.WallEast)];
        SeedWallTiles(recorder, expectedTiles);

        // Act
        region.FlagWallObject(gameObject);

        // Assert
        AssertWallState(recorder, expectedTiles, solid: false, gateway: false);
    }

    [TestMethod]
    public void FlagWallObject_Gateway_OmitsOnlyRangeLayer()
    {
        // Arrange
        var (region, recorder) = CreateRegion();
        var gameObject = CreateGameObject(ShapeType.Wall, 0, gateway: true, clipType: 0);
        ExpectedTile[] expectedTiles = [new(0, 0, CollisionFlag.WallWest), new(-1, 0, CollisionFlag.WallEast)];
        SeedWallTiles(recorder, expectedTiles);

        // Act
        region.FlagWallObject(gameObject);

        // Assert
        AssertWallState(recorder, expectedTiles, solid: true, gateway: true);
    }

    [TestMethod]
    [DataRow(ShapeType.Wall)]
    [DataRow(ShapeType.WallCornerDiagonal)]
    [DataRow(ShapeType.UnfinishedWall)]
    [DataRow(ShapeType.WallCorner)]
    public void FlagWallObject_NonClippedNonGateway_WritesNoCollision(ShapeType shapeType)
    {
        // Arrange
        var (region, recorder) = CreateRegion();
        var gameObject = CreateGameObject(shapeType, 0, clipType: 0);

        // Act
        region.FlagWallObject(gameObject);

        // Assert
        Assert.IsEmpty(recorder.Tiles);
    }

    [TestMethod]
    public void FloorDecoration_WithClipTypeOne_FlagThenUnflag_RestoresUnrelatedCollision()
    {
        // Arrange
        var (region, _) = CreateRegion();
        var gameObject = CreateGameObject(ShapeType.GroundDecoration, 0, clipType: 1);
        region.SetCollision(OriginX, OriginY, Plane, UnrelatedFlag);

        // Act
        region.FlagFloorDecorationCollision(gameObject);

        // Assert
        Assert.AreEqual(UnrelatedFlag | CollisionFlag.FloorDecorationBlock, region.GetCollision(OriginX, OriginY, Plane));

        // Act
        region.UnFlagFloorDecorationCollision(gameObject);

        // Assert
        Assert.AreEqual(UnrelatedFlag, region.GetCollision(OriginX, OriginY, Plane));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(2)]
    public void FlagFloorDecorationCollision_UnsupportedClipType_WritesNoCollision(int clipType)
    {
        // Arrange
        var (region, _) = CreateRegion();
        var gameObject = CreateGameObject(ShapeType.GroundDecoration, 0, clipType: clipType);
        region.SetCollision(OriginX, OriginY, Plane, UnrelatedFlag);

        // Act
        region.FlagFloorDecorationCollision(gameObject);

        // Assert
        Assert.AreEqual(UnrelatedFlag, region.GetCollision(OriginX, OriginY, Plane));
    }

    [TestMethod]
    [DynamicData(nameof(StandardCases))]
    public void StandardObject_FlagThenUnflag_WritesRotatedFootprintAndRestoresBaseline(StandardCase testCase)
    {
        // Arrange
        var (region, recorder) = CreateRegion();
        var gameObject = CreateGameObject(
            ShapeType.GroundDefault,
            testCase.Rotation,
            testCase.Solid,
            testCase.Gateway,
            clipType: 1,
            sizeX: 2,
            sizeY: 3);
        var footprint = GetStandardFootprint(testCase.Rotation);
        foreach (var (x, y) in footprint)
        {
            recorder.Seed(Location.Create(x, y, Plane, Dimension), UnrelatedFlag);
        }
        recorder.Seed(Location.Create(20, 20, Plane, Dimension), CollisionFlag.ObjectTile);

        // Act
        region.FlagStandardObject(gameObject);

        // Assert
        foreach (var (x, y) in footprint)
        {
            Assert.AreEqual(UnrelatedFlag | testCase.ExpectedFlags, recorder.Get(x, y, Plane, Dimension));
        }
        Assert.HasCount(footprint.Count + 1, recorder.Tiles);
        Assert.AreEqual(CollisionFlag.ObjectTile, recorder.Get(20, 20, Plane, Dimension));

        // Act
        region.UnFlagStandardObject(gameObject);

        // Assert
        foreach (var (x, y) in footprint)
        {
            Assert.AreEqual(UnrelatedFlag, recorder.Get(x, y, Plane, Dimension));
        }
        Assert.HasCount(footprint.Count + 1, recorder.Tiles);
        Assert.AreEqual(CollisionFlag.ObjectTile, recorder.Get(20, 20, Plane, Dimension));
    }

    [TestMethod]
    public void FlagStandardObject_NonClippedNonGateway_WritesNoCollision()
    {
        // Arrange
        var (region, recorder) = CreateRegion();
        var gameObject = CreateGameObject(ShapeType.GroundDefault, 0, clipType: 0, sizeX: 2, sizeY: 3);
        foreach (var (x, y) in GetStandardFootprint(0))
        {
            recorder.Seed(Location.Create(x, y, Plane, Dimension), UnrelatedFlag);
        }

        // Act
        region.FlagStandardObject(gameObject);

        // Assert
        foreach (var (x, y) in GetStandardFootprint(0))
        {
            Assert.AreEqual(UnrelatedFlag, recorder.Get(x, y, Plane, Dimension));
        }
        Assert.HasCount(GetStandardFootprint(0).Count, recorder.Tiles);
    }

    [TestMethod]
    public void CollisionDispatcher_EachLayer_UsesItsMappedWriter()
    {
        // Arrange
        var (region, recorder) = CreateRegion();
        var wall = CreateGameObject(ShapeType.Wall, 0);
        var standardObject = CreateGameObject(ShapeType.GroundDefault, 0);
        var floorDecoration = CreateGameObject(ShapeType.GroundDecoration, 0, clipType: 1);
        var wallDecoration = CreateGameObject(ShapeType.WallDecorationStraightXOffset, 0);
        ExpectedTile[] wallTiles = [new(0, 0, CollisionFlag.WallWest), new(-1, 0, CollisionFlag.WallEast)];
        SeedWallTiles(recorder, wallTiles);
        region.SetCollision(30, 30, Plane, UnrelatedFlag);
        region.SetCollision(40, 40, Plane, UnrelatedFlag);
        standardObject.Location.Returns(Location.Create(20, 20, Plane, Dimension));
        floorDecoration.Location.Returns(Location.Create(30, 30, Plane, Dimension));
        wallDecoration.Location.Returns(Location.Create(40, 40, Plane, Dimension));

        // Act
        region.FlagCollision(wall);

        // Assert
        AssertWallState(recorder, wallTiles, solid: true, gateway: false);

        // Act
        region.UnFlagCollision(wall);

        // Assert
        AssertWallBaseline(recorder, wallTiles);

        // Act
        recorder.Seed(Location.Create(20, 20, Plane, Dimension), UnrelatedFlag);
        region.FlagCollision(standardObject);
        region.FlagCollision(floorDecoration);
        region.FlagCollision(wallDecoration);

        // Assert
        Assert.AreEqual(UnrelatedFlag | CollisionFlag.ObjectTile | CollisionFlag.ObjectBlock | CollisionFlag.ObjectAllowRange, recorder.Get(20, 20, Plane, Dimension));
        Assert.AreEqual(UnrelatedFlag | CollisionFlag.FloorDecorationBlock, region.GetCollision(30, 30, Plane));
        Assert.AreEqual(UnrelatedFlag, region.GetCollision(40, 40, Plane));

        // Act
        region.UnFlagCollision(standardObject);
        region.UnFlagCollision(floorDecoration);
        region.UnFlagCollision(wallDecoration);

        // Assert
        Assert.AreEqual(UnrelatedFlag, recorder.Get(20, 20, Plane, Dimension));
        Assert.AreEqual(UnrelatedFlag, region.GetCollision(30, 30, Plane));
        Assert.AreEqual(UnrelatedFlag, region.GetCollision(40, 40, Plane));
    }

    private static TestDataRow<WallCase> WallCaseFor(ShapeType shapeType, int rotation, ExpectedTile[] tiles) =>
        new(new WallCase(shapeType, rotation, tiles)) { DisplayName = $"{shapeType} rotation {rotation}" };

    private static TestDataRow<StandardCase> StandardCaseFor(int rotation, bool solid, bool gateway)
    {
        var expectedFlags = CollisionFlag.ObjectTile;
        if (solid)
        {
            expectedFlags |= CollisionFlag.ObjectBlock;
        }
        if (!gateway)
        {
            expectedFlags |= CollisionFlag.ObjectAllowRange;
        }

        return new(new StandardCase(rotation, solid, gateway, expectedFlags))
        {
            DisplayName = $"rotation {rotation}, solid {solid}, gateway {gateway}"
        };
    }

    private static (MapRegion Region, CollisionRecorder Recorder) CreateRegion()
    {
        var mapRegionService = Substitute.For<IMapRegionService>();
        var recorder = new CollisionRecorder();
        mapRegionService.When(service => service.FlagCollision(Arg.Any<ILocation>(), Arg.Any<CollisionFlag>()))
            .Do(call => recorder.Flag(call.Arg<ILocation>()!, call.Arg<CollisionFlag>()));
        mapRegionService.When(service => service.UnFlagCollision(Arg.Any<ILocation>(), Arg.Any<CollisionFlag>()))
            .Do(call => recorder.UnFlag(call.Arg<ILocation>()!, call.Arg<CollisionFlag>()));

        var region = new MapRegion(
            Location.Zero,
            new int[4],
            Substitute.For<INpcService>(),
            mapRegionService,
            Substitute.For<IGameObjectBuilder>(),
            Substitute.For<IGroundItemBuilder>(),
            Substitute.For<IMapper>());

        return (region, recorder);
    }

    private static IGameObject CreateGameObject(
        ShapeType shapeType,
        int rotation,
        bool solid = true,
        bool gateway = false,
        int clipType = 1,
        int sizeX = 1,
        int sizeY = 1)
    {
        var definition = Substitute.For<IGameObjectDefinition>();
        definition.Solid.Returns(solid);
        definition.Gateway.Returns(gateway);
        definition.ClipType.Returns(clipType);
        definition.SizeX.Returns(sizeX);
        definition.SizeY.Returns(sizeY);

        var gameObject = Substitute.For<IGameObject>();
        gameObject.ShapeType.Returns(shapeType);
        gameObject.Rotation.Returns(rotation);
        gameObject.Location.Returns(Location.Create(OriginX, OriginY, Plane, Dimension));
        gameObject.Definition.Returns(definition);
        return gameObject;
    }

    private static void SeedWallTiles(CollisionRecorder recorder, IEnumerable<ExpectedTile> tiles)
    {
        foreach (var tile in tiles)
        {
            recorder.Seed(Location.Create(OriginX + tile.XOffset, OriginY + tile.YOffset, Plane, Dimension), UnrelatedFlag);
        }
    }

    private static void AssertWallState(CollisionRecorder recorder, IReadOnlyList<ExpectedTile> expectedTiles, bool solid, bool gateway)
    {
        Assert.HasCount(expectedTiles.Count, recorder.Tiles);

        foreach (var tile in expectedTiles)
        {
            var actual = recorder.Get(OriginX + tile.XOffset, OriginY + tile.YOffset, Plane, Dimension);
            var expectedBlocked = solid ? ToBlockedFlags(tile.WallFlags) : CollisionFlag.Walkable;
            var expectedRange = gateway ? CollisionFlag.Walkable : ToRangeFlags(tile.WallFlags);
            var expected = UnrelatedFlag | tile.WallFlags | expectedBlocked | expectedRange;

            Assert.AreEqual(tile.WallFlags, actual & WallLayerMask, $"Low wall layer at ({tile.XOffset}, {tile.YOffset})");
            Assert.AreEqual(expectedBlocked, actual & BlockedLayerMask, $"Solid wall layer at ({tile.XOffset}, {tile.YOffset})");
            Assert.AreEqual(expectedRange, actual & RangeLayerMask, $"Range wall layer at ({tile.XOffset}, {tile.YOffset})");
            Assert.AreEqual(expected, actual, $"Exact collision flags at ({tile.XOffset}, {tile.YOffset})");
        }
    }

    private static void AssertWallBaseline(CollisionRecorder recorder, IReadOnlyList<ExpectedTile> expectedTiles)
    {
        Assert.HasCount(expectedTiles.Count, recorder.Tiles);

        foreach (var tile in expectedTiles)
        {
            Assert.AreEqual(UnrelatedFlag, recorder.Get(OriginX + tile.XOffset, OriginY + tile.YOffset, Plane, Dimension));
        }
    }

    private static CollisionFlag ToBlockedFlags(CollisionFlag wallFlags)
    {
        var result = CollisionFlag.Walkable;
        if ((wallFlags & CollisionFlag.WallNorthWest) != 0) result |= CollisionFlag.BlockedNorthWest;
        if ((wallFlags & CollisionFlag.WallNorth) != 0) result |= CollisionFlag.BlockedNorth;
        if ((wallFlags & CollisionFlag.WallNorthEast) != 0) result |= CollisionFlag.BlockedNorthEast;
        if ((wallFlags & CollisionFlag.WallEast) != 0) result |= CollisionFlag.BlockedEast;
        if ((wallFlags & CollisionFlag.WallSouthEast) != 0) result |= CollisionFlag.BlockedSouthEast;
        if ((wallFlags & CollisionFlag.WallSouth) != 0) result |= CollisionFlag.BlockedSouth;
        if ((wallFlags & CollisionFlag.WallSouthWest) != 0) result |= CollisionFlag.BlockedSouthWest;
        if ((wallFlags & CollisionFlag.WallWest) != 0) result |= CollisionFlag.BlockedWest;
        return result;
    }

    private static CollisionFlag ToRangeFlags(CollisionFlag wallFlags)
    {
        var result = CollisionFlag.Walkable;
        if ((wallFlags & CollisionFlag.WallNorthWest) != 0) result |= CollisionFlag.WallAllowRangeNorthWest;
        if ((wallFlags & CollisionFlag.WallNorth) != 0) result |= CollisionFlag.WallAllowRangeNorth;
        if ((wallFlags & CollisionFlag.WallNorthEast) != 0) result |= CollisionFlag.WallAllowRangeNorthEast;
        if ((wallFlags & CollisionFlag.WallEast) != 0) result |= CollisionFlag.WallAllowRangeEast;
        if ((wallFlags & CollisionFlag.WallSouthEast) != 0) result |= CollisionFlag.WallAllowRangeSouthEast;
        if ((wallFlags & CollisionFlag.WallSouth) != 0) result |= CollisionFlag.WallAllowRangeSouth;
        if ((wallFlags & CollisionFlag.WallSouthWest) != 0) result |= CollisionFlag.WallAllowRangeSouthWest;
        if ((wallFlags & CollisionFlag.WallWest) != 0) result |= CollisionFlag.WallAllowRangeWest;
        return result;
    }

    private static IReadOnlyList<(int X, int Y)> GetStandardFootprint(int rotation)
    {
        var sizeX = rotation is 1 or 3 ? 3 : 2;
        var sizeY = rotation is 1 or 3 ? 2 : 3;
        var footprint = new List<(int X, int Y)>();

        for (var x = OriginX; x < OriginX + sizeX; x++)
        {
            for (var y = OriginY; y < OriginY + sizeY; y++)
            {
                footprint.Add((x, y));
            }
        }

        return footprint;
    }

    public sealed record WallCase(ShapeType ShapeType, int Rotation, IReadOnlyList<ExpectedTile> Tiles);

    public sealed record StandardCase(int Rotation, bool Solid, bool Gateway, CollisionFlag ExpectedFlags);

    public readonly record struct ExpectedTile(int XOffset, int YOffset, CollisionFlag WallFlags);

    private sealed class CollisionRecorder
    {
        private readonly Dictionary<(int X, int Y, int Z, int Dimension), CollisionFlag> _tiles = [];

        public IReadOnlyDictionary<(int X, int Y, int Z, int Dimension), CollisionFlag> Tiles => _tiles;

        public void Seed(ILocation location, CollisionFlag flags) => Flag(location, flags);

        public void Flag(ILocation location, CollisionFlag flags)
        {
            var key = (location.X, location.Y, location.Z, location.Dimension);
            _tiles[key] = Get(location.X, location.Y, location.Z, location.Dimension) | flags;
        }

        public void UnFlag(ILocation location, CollisionFlag flags)
        {
            var key = (location.X, location.Y, location.Z, location.Dimension);
            _tiles[key] = Get(location.X, location.Y, location.Z, location.Dimension) & ~flags;
        }

        public CollisionFlag Get(int x, int y, int z, int dimension) =>
            _tiles.GetValueOrDefault((x, y, z, dimension), CollisionFlag.Walkable);
    }
}
