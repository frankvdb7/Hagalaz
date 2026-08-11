using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using NSubstitute;
using SmartPath = Hagalaz.Services.GameWorld.Logic.Pathfinding.Path;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class SmartPathfinderDirectionalTraversalTests
{
    private const int GraphSize = 104;
    private const int FromX = 50;
    private const int FromY = 50;

    public sealed record TraversalCell(int XOffset, int YOffset, CollisionFlag MatchingBlocker);

    public sealed record TraversalCase(string Name, int XOffset, int YOffset, TraversalCell[] IncomingCells);

    public enum PositiveDirection
    {
        East,
        North,
        NorthEast,
        NorthWest,
        SouthEast
    }

    public static IEnumerable<TestDataRow<TraversalCase>> SingleTraversalCases =>
    [
        Case("south", 0, -1, [new(0, -1, CollisionFlag.WallAllowRangeNorth)]),
        Case("west", -1, 0, [new(-1, 0, CollisionFlag.WallAllowRangeEast)]),
        Case("north", 0, 1, [new(0, 1, CollisionFlag.WallAllowRangeSouth)]),
        Case("east", 1, 0, [new(1, 0, CollisionFlag.WallAllowRangeWest)]),
        Case("south-west", -1, -1,
        [
            new(-1, -1, CollisionFlag.WallAllowRangeSouthWest),
            new(-1, 0, CollisionFlag.WallAllowRangeEast),
            new(0, -1, CollisionFlag.WallAllowRangeNorth)
        ]),
        Case("north-west", -1, 1,
        [
            new(-1, 1, CollisionFlag.WallAllowRangeNorthWest),
            new(-1, 0, CollisionFlag.WallAllowRangeEast),
            new(0, 1, CollisionFlag.WallAllowRangeSouth)
        ]),
        Case("south-east", 1, -1,
        [
            new(1, -1, CollisionFlag.WallAllowRangeSouthEast),
            new(1, 0, CollisionFlag.WallAllowRangeWest),
            new(0, -1, CollisionFlag.WallAllowRangeNorth)
        ]),
        Case("north-east", 1, 1,
        [
            new(1, 1, CollisionFlag.WallAllowRangeNorthEast),
            new(1, 0, CollisionFlag.WallAllowRangeWest),
            new(0, 1, CollisionFlag.WallAllowRangeSouth)
        ])
    ];

    public static IEnumerable<TestDataRow<TraversalCase>> DoubleTraversalCases =>
    [
        Case("south", 0, -1,
        [
            new(0, -1, CollisionFlag.WallAllowRangeSouthWest),
            new(1, -1, CollisionFlag.WallAllowRangeSouthEast)
        ]),
        Case("west", -1, 0,
        [
            new(-1, 0, CollisionFlag.WallAllowRangeSouthWest),
            new(-1, 1, CollisionFlag.WallAllowRangeNorthWest)
        ]),
        Case("north", 0, 1,
        [
            new(0, 2, CollisionFlag.WallAllowRangeNorthWest),
            new(1, 2, CollisionFlag.WallAllowRangeNorthEast)
        ]),
        Case("east", 1, 0,
        [
            new(2, 0, CollisionFlag.WallAllowRangeSouthEast),
            new(2, 1, CollisionFlag.WallAllowRangeNorthEast)
        ]),
        Case("south-west", -1, -1,
        [
            new(-1, -1, CollisionFlag.WallAllowRangeSouthWest),
            new(-1, 0, CollisionFlag.WallAllowRangeNorthWest),
            new(0, -1, CollisionFlag.WallAllowRangeSouthEast)
        ]),
        Case("north-west", -1, 1,
        [
            new(-1, 1, CollisionFlag.WallAllowRangeSouthWest),
            new(-1, 2, CollisionFlag.WallAllowRangeNorthWest),
            new(0, 2, CollisionFlag.WallAllowRangeNorthEast)
        ]),
        Case("south-east", 1, -1,
        [
            new(1, -1, CollisionFlag.WallAllowRangeSouthWest),
            new(2, 0, CollisionFlag.WallAllowRangeNorthEast),
            new(2, -1, CollisionFlag.WallAllowRangeSouthEast)
        ]),
        Case("north-east", 1, 1,
        [
            new(1, 2, CollisionFlag.WallAllowRangeNorthWest),
            new(2, 2, CollisionFlag.WallAllowRangeNorthEast),
            new(2, 1, CollisionFlag.WallAllowRangeSouthEast)
        ])
    ];

    public static IEnumerable<TestDataRow<(int Size, int XOffset, int YOffset)>> VariableDiagonalCases =>
    [
        new((3, -1, -1)) { DisplayName = "size 3 south-west" },
        new((4, -1, -1)) { DisplayName = "size 4 south-west" },
        new((3, 1, -1)) { DisplayName = "size 3 south-east" },
        new((4, 1, -1)) { DisplayName = "size 4 south-east" }
    ];

    public static IEnumerable<TestDataRow<(int Size, PositiveDirection Direction)>> PositiveBoundaryCases =>
    [
        new((3, PositiveDirection.East)),
        new((3, PositiveDirection.North)),
        new((3, PositiveDirection.NorthEast)),
        new((3, PositiveDirection.NorthWest)),
        new((3, PositiveDirection.SouthEast)),
        new((4, PositiveDirection.East)),
        new((4, PositiveDirection.North)),
        new((4, PositiveDirection.NorthEast)),
        new((4, PositiveDirection.NorthWest)),
        new((4, PositiveDirection.SouthEast))
    ];

    [TestMethod]
    [DynamicData(nameof(SingleTraversalCases))]
    public void Find_SizeOne_ClearDirection_ReconstructsDirectPath(TraversalCase traversal)
    {
        var target = TargetFor(traversal);
        var result = FindWithConstrainedCollision(1, target, traversal.IncomingCells);

        AssertDirectPath(result.Path);
    }

    [TestMethod]
    [DynamicData(nameof(SingleTraversalCases))]
    public void Find_SizeOne_MatchingBlocker_PreventsDirectExpansion(TraversalCase traversal)
    {
        var target = TargetFor(traversal);

        foreach (var blockedCell in traversal.IncomingCells)
        {
            var result = FindWithConstrainedCollision(1, target, traversal.IncomingCells, blockedCell, blockedCell.MatchingBlocker);

            AssertDoesNotTakeDirectExpansion(result.Path);
        }
    }

    [TestMethod]
    [DynamicData(nameof(SingleTraversalCases))]
    public void Find_SizeOne_NonMatchingDestinationBlocker_DoesNotPreventDirectExpansion(TraversalCase traversal)
    {
        var target = TargetFor(traversal);
        var destination = traversal.IncomingCells[0];
        var result = FindWithConstrainedCollision(
            1,
            target,
            traversal.IncomingCells,
            destination,
            OppositeDirectionalBlocker(destination.MatchingBlocker));

        AssertDirectPath(result.Path);
    }

    [TestMethod]
    [DynamicData(nameof(DoubleTraversalCases))]
    public void Find_SizeTwo_ClearIncomingFootprint_ReconstructsDirectPath(TraversalCase traversal)
    {
        var target = TargetFor(traversal);
        var result = FindWithConstrainedCollision(2, target, traversal.IncomingCells);

        AssertDirectPath(result.Path);
    }

    [TestMethod]
    [DynamicData(nameof(DoubleTraversalCases))]
    public void Find_SizeTwo_MatchingBlockerOnEveryIncomingCell_PreventsDirectExpansion(TraversalCase traversal)
    {
        var target = TargetFor(traversal);

        foreach (var blockedCell in traversal.IncomingCells)
        {
            var result = FindWithConstrainedCollision(2, target, traversal.IncomingCells, blockedCell, blockedCell.MatchingBlocker);

            AssertDoesNotTakeDirectExpansion(result.Path);
        }
    }

    [TestMethod]
    [DynamicData(nameof(DoubleTraversalCases))]
    public void Find_SizeTwo_NonMatchingBlockerOnEveryIncomingCell_DoesNotPreventDirectExpansion(TraversalCase traversal)
    {
        var target = TargetFor(traversal);

        foreach (var blockedCell in traversal.IncomingCells)
        {
            var result = FindWithConstrainedCollision(
                2,
                target,
                traversal.IncomingCells,
                blockedCell,
                OppositeDirectionalBlocker(blockedCell.MatchingBlocker));

            AssertDirectPath(result.Path);
        }
    }

    [TestMethod]
    [DynamicData(nameof(VariableDiagonalCases))]
    public void Find_VariableSize_DiagonalTraversal_ReconstructsTheCollisionValidatedAnchor(int size, int xOffset, int yOffset)
    {
        var target = Location.Create(FromX + xOffset, FromY + yOffset, 0);
        var result = FindWithConstrainedCollision(size, target, VariableDiagonalIncomingCells(size, xOffset, yOffset));

        AssertDirectPath(result.Path);
    }

    [TestMethod]
    [DynamicData(nameof(PositiveBoundaryCases))]
    public void Find_VariableSize_PositiveExpansionBeyondGraphBoundary_IsNotQueuedOrRead(int size, PositiveDirection direction)
    {
        var (target, walkableCells) = CreatePositiveBoundaryCorridor(size, direction);
        var result = FindWithConstrainedCollision(size, target, walkableCells);

        Assert.IsFalse(result.Path.Successful);
        Assert.IsTrue(result.ClippingLookups.All(cell => cell.X is >= 0 and < GraphSize && cell.Y is >= 0 and < GraphSize));
    }

    private static TestDataRow<TraversalCase> Case(string name, int xOffset, int yOffset, TraversalCell[] incomingCells) =>
        new(new TraversalCase(name, xOffset, yOffset, incomingCells)) { DisplayName = name };

    private static Location TargetFor(TraversalCase traversal) => Location.Create(FromX + traversal.XOffset, FromY + traversal.YOffset, 0);

    private static RouteResult FindWithConstrainedCollision(
        int selfSize,
        Location target,
        IEnumerable<TraversalCell> walkableCells,
        TraversalCell? blockedCell = null,
        CollisionFlag blocker = CollisionFlag.Walkable)
    {
        var clippingFlags = walkableCells
            .GroupBy(cell => (FromX + cell.XOffset, FromY + cell.YOffset))
            .ToDictionary(group => group.Key, _ => CollisionFlag.Walkable);
        if (blockedCell is not null)
        {
            clippingFlags[(FromX + blockedCell.XOffset, FromY + blockedCell.YOffset)] = blocker;
        }

        var clippingLookups = new List<(int X, int Y)>();
        var mapRegionService = Substitute.For<IMapRegionService>();
        mapRegionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(callInfo =>
            {
                var coordinate = (callInfo.ArgAt<int>(0), callInfo.ArgAt<int>(1));
                clippingLookups.Add(coordinate);
                return clippingFlags.GetValueOrDefault(coordinate, CollisionFlag.FloorBlock);
            });

        var pathFinder = new SmartPathFinder(mapRegionService);
        var path = pathFinder.Find(Location.Create(FromX, FromY, 0), selfSize, target, 0, 0, 0, 0, 0, false);

        return new RouteResult(path, clippingLookups);
    }

    private static IEnumerable<TraversalCell> VariableDiagonalIncomingCells(int size, int xOffset, int yOffset)
    {
        if (xOffset == -1 && yOffset == -1)
        {
            yield return new TraversalCell(-1, size - 2, CollisionFlag.Walkable);
            yield return new TraversalCell(-1, -1, CollisionFlag.Walkable);
            yield return new TraversalCell(size - 2, -1, CollisionFlag.Walkable);

            for (var i = 1; i < size - 1; i++)
            {
                yield return new TraversalCell(-1, i - 1, CollisionFlag.Walkable);
                yield return new TraversalCell(i - 1, -1, CollisionFlag.Walkable);
            }

            yield break;
        }

        if (xOffset == 1 && yOffset == -1)
        {
            yield return new TraversalCell(1, -1, CollisionFlag.Walkable);
            yield return new TraversalCell(size, -1, CollisionFlag.Walkable);
            yield return new TraversalCell(size, size - 2, CollisionFlag.Walkable);

            for (var i = 1; i < size - 1; i++)
            {
                yield return new TraversalCell(size, i - 1, CollisionFlag.Walkable);
                yield return new TraversalCell(i + 1, -1, CollisionFlag.Walkable);
            }

            yield break;
        }

        throw new ArgumentOutOfRangeException(nameof(xOffset));
    }

    private static (Location Target, IEnumerable<TraversalCell> WalkableCells) CreatePositiveBoundaryCorridor(int size, PositiveDirection direction)
    {
        var finalAnchor = GraphSize - size;
        return direction switch
        {
            PositiveDirection.East =>
                (Location.Create(finalAnchor + 1, FromY, 0), CardinalEastCorridor(size, finalAnchor)),
            PositiveDirection.North =>
                (Location.Create(FromX, finalAnchor + 1, 0), CardinalNorthCorridor(size, finalAnchor)),
            PositiveDirection.NorthEast =>
                (Location.Create(finalAnchor + 1, finalAnchor + 1, 0), DiagonalNorthEastCorridor(size, finalAnchor)),
            PositiveDirection.NorthWest =>
                (Location.Create(finalAnchor - 1, finalAnchor + 1, 0), NorthWestBoundaryCorridor(size, finalAnchor)),
            PositiveDirection.SouthEast =>
                (Location.Create(finalAnchor + 1, FromY - 1, 0), SouthEastBoundaryCorridor(size, finalAnchor)),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
    }

    private static IEnumerable<TraversalCell> CardinalEastCorridor(int size, int finalAnchor)
    {
        for (var x = FromX; x <= finalAnchor; x++)
        {
            for (var y = 0; y < size; y++)
            {
                yield return new TraversalCell(x + size - FromX, y, CollisionFlag.Walkable);
            }
        }
    }

    private static IEnumerable<TraversalCell> CardinalNorthCorridor(int size, int finalAnchor)
    {
        for (var y = FromY; y <= finalAnchor; y++)
        {
            for (var x = 0; x < size; x++)
            {
                yield return new TraversalCell(x, y + size - FromY, CollisionFlag.Walkable);
            }
        }
    }

    private static IEnumerable<TraversalCell> DiagonalNorthEastCorridor(int size, int finalAnchor)
    {
        for (var anchor = FromX; anchor <= finalAnchor; anchor++)
        {
            foreach (var cell in VariableNorthEastIncomingCells(size, anchor, anchor))
            {
                yield return cell;
            }
        }
    }

    private static IEnumerable<TraversalCell> NorthWestBoundaryCorridor(int size, int finalAnchor)
    {
        for (var anchor = FromX; anchor < finalAnchor; anchor++)
        {
            foreach (var cell in VariableNorthEastIncomingCells(size, anchor, anchor))
            {
                yield return cell;
            }
        }

        foreach (var cell in VariableNorthWestIncomingCells(size, finalAnchor, finalAnchor))
        {
            yield return cell;
        }
    }

    private static IEnumerable<TraversalCell> SouthEastBoundaryCorridor(int size, int finalAnchor)
    {
        for (var anchor = FromX; anchor < finalAnchor; anchor++)
        {
            for (var y = 0; y < size; y++)
            {
                yield return new TraversalCell(anchor + size - FromX, y, CollisionFlag.Walkable);
            }
        }

        foreach (var cell in VariableSouthEastIncomingCells(size, finalAnchor, FromY))
        {
            yield return cell;
        }
    }

    private static IEnumerable<TraversalCell> VariableNorthEastIncomingCells(int size, int anchorX, int anchorY)
    {
        yield return Offset(anchorX + 1, anchorY + size);
        yield return Offset(anchorX + size, anchorY + size);
        yield return Offset(anchorX + size, anchorY + 1);

        for (var i = 1; i < size - 1; i++)
        {
            yield return Offset(anchorX + i + 1, anchorY + size);
            yield return Offset(anchorX + size, anchorY + i + 1);
        }
    }

    private static IEnumerable<TraversalCell> VariableNorthWestIncomingCells(int size, int anchorX, int anchorY)
    {
        yield return Offset(anchorX - 1, anchorY + 1);
        yield return Offset(anchorX - 1, anchorY + size);
        yield return Offset(anchorX, anchorY + size);

        for (var i = 1; i < size - 1; i++)
        {
            yield return Offset(anchorX - 1, anchorY + i - 1);
            yield return Offset(anchorX + i - 1, anchorY + size);
        }
    }

    private static IEnumerable<TraversalCell> VariableSouthEastIncomingCells(int size, int anchorX, int anchorY)
    {
        yield return Offset(anchorX + 1, anchorY - 1);
        yield return Offset(anchorX + size, anchorY - 1);
        yield return Offset(anchorX + size, anchorY + size - 2);

        for (var i = 1; i < size - 1; i++)
        {
            yield return Offset(anchorX + size, anchorY + i - 1);
            yield return Offset(anchorX + i + 1, anchorY - 1);
        }
    }

    private static TraversalCell Offset(int x, int y) => new(x - FromX, y - FromY, CollisionFlag.Walkable);

    private static CollisionFlag OppositeDirectionalBlocker(CollisionFlag matchingBlocker) => matchingBlocker switch
    {
        CollisionFlag.WallAllowRangeNorth => CollisionFlag.WallAllowRangeSouth,
        CollisionFlag.WallAllowRangeSouth => CollisionFlag.WallAllowRangeNorth,
        CollisionFlag.WallAllowRangeEast => CollisionFlag.WallAllowRangeWest,
        CollisionFlag.WallAllowRangeWest => CollisionFlag.WallAllowRangeEast,
        CollisionFlag.WallAllowRangeSouthWest => CollisionFlag.WallAllowRangeNorthEast,
        CollisionFlag.WallAllowRangeNorthEast => CollisionFlag.WallAllowRangeSouthWest,
        CollisionFlag.WallAllowRangeNorthWest => CollisionFlag.WallAllowRangeSouthEast,
        CollisionFlag.WallAllowRangeSouthEast => CollisionFlag.WallAllowRangeNorthWest,
        _ => throw new ArgumentOutOfRangeException(nameof(matchingBlocker))
    };

    private static void AssertDirectPath(IPath path)
    {
        Assert.IsTrue(path.Successful);
        Assert.AreEqual(1, ((SmartPath)path).Steps);
    }

    private static void AssertDoesNotTakeDirectExpansion(IPath path)
    {
        Assert.IsTrue(!path.Successful || ((SmartPath)path).Steps > 1);
    }

    private sealed record RouteResult(IPath Path, IReadOnlyList<(int X, int Y)> ClippingLookups);
}
