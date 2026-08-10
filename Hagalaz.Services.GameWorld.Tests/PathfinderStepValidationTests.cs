using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class PathfinderStepValidationTests
{
    private readonly IMapRegionService _mapRegionService;
    private readonly DumbPathFinder _pathFinder;

    public PathfinderStepValidationTests()
    {
        _mapRegionService = Substitute.For<IMapRegionService>();
        _mapRegionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(CollisionFlag.Walkable);
        _pathFinder = new DumbPathFinder(_mapRegionService);
    }

    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(1, 0)]
    [DataRow(0, -1)]
    [DataRow(0, 1)]
    [DataRow(-1, -1)]
    [DataRow(1, -1)]
    [DataRow(-1, 1)]
    [DataRow(1, 1)]
    public void CheckStep_SizeOne_UnitDirectionsRemainWalkable(int xOffset, int yOffset)
    {
        var result = _pathFinder.CheckStep(10 + xOffset, 10 + yOffset, 0, xOffset, yOffset, 1);

        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow(-1, 0, -1, 0)]
    [DataRow(1, 0, 2, 0)]
    [DataRow(0, -1, 0, -1)]
    [DataRow(0, 1, 0, 2)]
    [DataRow(-1, -1, -1, -1)]
    [DataRow(1, -1, 1, -1)]
    [DataRow(-1, 1, -1, 2)]
    [DataRow(1, 1, 1, 2)]
    public void CheckStep_SizeTwo_RejectsBlockedIncomingFootprint(
        int xOffset,
        int yOffset,
        int blockedOffsetX,
        int blockedOffsetY)
    {
        _mapRegionService.GetClippingFlag(10 + blockedOffsetX, 10 + blockedOffsetY, 0)
            .Returns(CollisionFlag.FloorBlock);

        var result = _pathFinder.CheckStep(10 + xOffset, 10 + yOffset, 0, xOffset, yOffset, 2);

        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow(3, -1, 0, -1, 1)]
    [DataRow(4, -1, 0, -1, 2)]
    [DataRow(3, 1, 0, 3, 1)]
    [DataRow(4, 1, 0, 4, 2)]
    [DataRow(3, 0, -1, 1, -1)]
    [DataRow(4, 0, -1, 2, -1)]
    [DataRow(3, 0, 1, 1, 3)]
    [DataRow(4, 0, 1, 2, 4)]
    public void CheckStep_VariableSize_RejectsBlockedCardinalInterior(
        int size,
        int xOffset,
        int yOffset,
        int blockedOffsetX,
        int blockedOffsetY)
    {
        _mapRegionService.GetClippingFlag(10 + blockedOffsetX, 10 + blockedOffsetY, 0)
            .Returns(CollisionFlag.FloorBlock);

        var result = _pathFinder.CheckStep(10 + xOffset, 10 + yOffset, 0, xOffset, yOffset, size);

        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow(3, -1, 0, -1, 0)]
    [DataRow(4, 1, 0, 4, 0)]
    [DataRow(3, 0, -1, 0, -1)]
    [DataRow(4, 0, 1, 0, 4)]
    public void CheckStep_VariableSize_RejectsBlockedCardinalCorner(
        int size,
        int xOffset,
        int yOffset,
        int blockedOffsetX,
        int blockedOffsetY)
    {
        _mapRegionService.GetClippingFlag(10 + blockedOffsetX, 10 + blockedOffsetY, 0)
            .Returns(CollisionFlag.FloorBlock);

        var result = _pathFinder.CheckStep(10 + xOffset, 10 + yOffset, 0, xOffset, yOffset, size);

        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow(3, -1, -1, -1, -1)]
    [DataRow(4, 1, -1, 4, -1)]
    [DataRow(3, -1, 1, -1, 3)]
    [DataRow(4, 1, 1, 4, 4)]
    public void CheckStep_VariableSize_RejectsBlockedDiagonalCorner(
        int size,
        int xOffset,
        int yOffset,
        int blockedOffsetX,
        int blockedOffsetY)
    {
        _mapRegionService.GetClippingFlag(10 + blockedOffsetX, 10 + blockedOffsetY, 0)
            .Returns(CollisionFlag.FloorBlock);

        var result = _pathFinder.CheckStep(10 + xOffset, 10 + yOffset, 0, xOffset, yOffset, size);

        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow(-2, 0)]
    [DataRow(2, 0)]
    [DataRow(0, -2)]
    [DataRow(0, 2)]
    [DataRow(-2, -2)]
    [DataRow(2, 2)]
    [DataRow(0, 0)]
    public void CheckStep_UnsupportedOffset_ReturnsFalse(int xOffset, int yOffset)
    {
        var result = _pathFinder.CheckStep(10 + xOffset, 10 + yOffset, 0, xOffset, yOffset, 1);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void CheckStep_DistanceCheck_UsesUnitSteps()
    {
        _mapRegionService.GetClippingFlag(10, 10, 0).Returns(CollisionFlag.FloorBlock);

        var result = _pathFinder.CheckStep(Location.Create(10, 10, 0), 1);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void CheckStep_DistanceCheck_WithWalkableUnitSteps_ReturnsTrue()
    {
        var result = _pathFinder.CheckStep(Location.Create(10, 10, 0), 1);

        Assert.IsTrue(result);
    }
}
