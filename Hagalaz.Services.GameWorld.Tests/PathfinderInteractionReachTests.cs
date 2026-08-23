using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class PathfinderInteractionReachTests
{
    private const int CurrentX = 41;
    private const int CurrentY = 73;
    private const int Plane = 0;

    private readonly IMapRegionService _mapRegionService;
    private readonly DumbPathFinder _pathFinder;

    public PathfinderInteractionReachTests()
    {
        _mapRegionService = Substitute.For<IMapRegionService>();
        _mapRegionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(CollisionFlag.Walkable);
        _pathFinder = new DumbPathFinder(_mapRegionService);
    }

    public static IEnumerable<TestDataRow<OverlapCase>> LargeEntityOutsideFootprintCases =>
        CreateLargeEntityOutsideFootprintCases()
            .Select(testCase => new TestDataRow<OverlapCase>(testCase) { DisplayName = testCase.Name });

    public static IEnumerable<TestDataRow<DoorInteractionCase>> LargeDoorInteractionCases =>
        CreateLargeDoorInteractionCases()
            .Select(testCase => new TestDataRow<DoorInteractionCase>(testCase) { DisplayName = testCase.Name });

    public static IEnumerable<TestDataRow<DecorationInteractionCase>> LargeDecorationInteractionCases =>
        CreateLargeDecorationInteractionCases()
            .Select(testCase => new TestDataRow<DecorationInteractionCase>(testCase) { DisplayName = testCase.Name });

    [TestMethod]
    [DynamicData(nameof(LargeEntityOutsideFootprintCases))]
    public void CanDecorationInteract_LargeEntityTargetOutsideEitherFootprintAxis_ReturnsFalse(OverlapCase testCase)
    {
        // Act
        var result = _pathFinder.CanDecorationInteract(
            shape: 99,
            CurrentX,
            CurrentY,
            testCase.TargetX,
            testCase.TargetY,
            testCase.Size,
            rotation: 0,
            Plane);

        // Assert
        Assert.IsFalse(result, testCase.Name);
    }

    [TestMethod]
    [DynamicData(nameof(LargeEntityOutsideFootprintCases))]
    public void CanDoorInteract_LargeEntityTargetOutsideEitherFootprintAxis_ReturnsFalse(OverlapCase testCase)
    {
        // Act
        var result = _pathFinder.CanDoorInteract(
            shape: 99,
            testCase.TargetX,
            testCase.TargetY,
            CurrentX,
            CurrentY,
            rotation: 0,
            testCase.Size,
            Plane);

        // Assert
        Assert.IsFalse(result, testCase.Name);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public void CanDecorationInteract_LargeEntityTargetInsideBothFootprintAxes_ReturnsTrue(int size)
    {
        // Act
        var result = _pathFinder.CanDecorationInteract(
            shape: 99,
            CurrentX,
            CurrentY,
            CurrentX + size - 1,
            CurrentY + size - 1,
            size,
            rotation: 0,
            Plane);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public void CanDoorInteract_LargeEntityTargetInsideBothFootprintAxes_ReturnsTrue(int size)
    {
        // Act
        var result = _pathFinder.CanDoorInteract(
            shape: 99,
            CurrentX + size - 1,
            CurrentY + size - 1,
            CurrentX,
            CurrentY,
            rotation: 0,
            size,
            Plane);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DynamicData(nameof(LargeDoorInteractionCases))]
    public void CanDoorInteract_LargeEntityActualSideIsBlocked_ReturnsFalse(DoorInteractionCase testCase)
    {
        // Arrange
        _mapRegionService.GetClippingFlag(testCase.CollisionX, testCase.CollisionY, Plane)
            .Returns(CollisionFlag.FloorBlock);

        // Act
        var result = _pathFinder.CanDoorInteract(
            testCase.Shape,
            testCase.TargetX,
            testCase.TargetY,
            CurrentX,
            CurrentY,
            testCase.Rotation,
            testCase.Size,
            Plane);

        // Assert
        Assert.IsFalse(result, testCase.Name);
    }

    [TestMethod]
    [DynamicData(nameof(LargeDoorInteractionCases))]
    public void CanDoorInteract_LargeEntityUnrelatedCollision_ReturnsTrue(DoorInteractionCase testCase)
    {
        // Arrange
        _mapRegionService.GetClippingFlag(testCase.CollisionX + 20, testCase.CollisionY + 20, Plane)
            .Returns(CollisionFlag.FloorBlock);

        // Act
        var result = _pathFinder.CanDoorInteract(
            testCase.Shape,
            testCase.TargetX,
            testCase.TargetY,
            CurrentX,
            CurrentY,
            testCase.Rotation,
            testCase.Size,
            Plane);

        // Assert
        Assert.IsTrue(result, testCase.Name);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public void CanDoorInteract_ShapeZeroRotationZero_UsesTargetXAndCurrentYForCollision(int size)
    {
        // Arrange
        _mapRegionService.GetClippingFlag(CurrentX, CurrentY, Plane)
            .Returns(CollisionFlag.FloorBlock);

        // Act
        var result = _pathFinder.CanDoorInteract(0, CurrentX, CurrentY - 1, CurrentX, CurrentY, 0, size, Plane);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public void CanDoorInteract_ShapeZeroRotationZero_IgnoresCollisionAtXUsedAsY(int size)
    {
        // Arrange
        _mapRegionService.GetClippingFlag(CurrentX, CurrentX, Plane)
            .Returns(CollisionFlag.FloorBlock);

        // Act
        var result = _pathFinder.CanDoorInteract(0, CurrentX, CurrentY - 1, CurrentX, CurrentY, 0, size, Plane);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DynamicData(nameof(LargeDecorationInteractionCases))]
    public void CanDecorationInteract_LargeEntityActualSideIsBlocked_ReturnsFalse(DecorationInteractionCase testCase)
    {
        // Arrange
        _mapRegionService.GetClippingFlag(testCase.CollisionX, testCase.CollisionY, Plane)
            .Returns(testCase.BlockingFlag);

        // Act
        var result = _pathFinder.CanDecorationInteract(
            testCase.Shape,
            CurrentX,
            CurrentY,
            testCase.TargetX,
            testCase.TargetY,
            testCase.Size,
            testCase.Rotation,
            Plane);

        // Assert
        Assert.IsFalse(result, testCase.Name);
    }

    [TestMethod]
    [DynamicData(nameof(LargeDecorationInteractionCases))]
    public void CanDecorationInteract_LargeEntityUnrelatedCollision_ReturnsTrue(DecorationInteractionCase testCase)
    {
        // Arrange
        _mapRegionService.GetClippingFlag(testCase.CollisionX + 20, testCase.CollisionY + 20, Plane)
            .Returns(testCase.BlockingFlag);

        // Act
        var result = _pathFinder.CanDecorationInteract(
            testCase.Shape,
            CurrentX,
            CurrentY,
            testCase.TargetX,
            testCase.TargetY,
            testCase.Size,
            testCase.Rotation,
            Plane);

        // Assert
        Assert.IsTrue(result, testCase.Name);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public void CanDecorationInteract_ShapeSixRotationZero_UsesWestSideTargetYForCollision(int size)
    {
        // Arrange
        _mapRegionService.GetClippingFlag(CurrentX, CurrentY, Plane)
            .Returns(CollisionFlag.WallWest);

        // Act
        var result = _pathFinder.CanDecorationInteract(6, CurrentX, CurrentY, CurrentX - 1, CurrentY, size, 0, Plane);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    public void CanDecorationInteract_ShapeSixRotationZero_IgnoresCollisionAtTargetXAsY(int size)
    {
        // Arrange
        _mapRegionService.GetClippingFlag(CurrentX, CurrentX - 1, Plane)
            .Returns(CollisionFlag.WallWest);

        // Act
        var result = _pathFinder.CanDecorationInteract(6, CurrentX, CurrentY, CurrentX - 1, CurrentY, size, 0, Plane);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow(0, CurrentX, CurrentY, 0)]
    [DataRow(2, CurrentX, CurrentY, 1)]
    [DataRow(9, CurrentX, CurrentY, 3)]
    public void CanDoorInteract_SizeOneValidSupportedApproach_ReturnsTrue(int shape, int targetX, int targetY, int rotation)
    {
        // Act
        var result = _pathFinder.CanDoorInteract(shape, targetX, targetY, CurrentX - 1, CurrentY, rotation, 1, Plane);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    [DataRow(6, CurrentX - 2, CurrentY, 0)]
    [DataRow(7, CurrentX - 2, CurrentY, 1)]
    [DataRow(8, CurrentX - 2, CurrentY, 0)]
    public void CanDecorationInteract_SizeOneValidSupportedApproach_ReturnsTrue(int shape, int targetX, int targetY, int rotation)
    {
        // Act
        var result = _pathFinder.CanDecorationInteract(shape, CurrentX - 1, CurrentY, targetX, targetY, 1, rotation, Plane);

        // Assert
        Assert.IsTrue(result);
    }

    private static IEnumerable<OverlapCase> CreateLargeEntityOutsideFootprintCases()
    {
        foreach (var size in new[] { 2, 3 })
        {
            yield return new OverlapCase($"size {size}, X inside and Y below", size, CurrentX, CurrentY - 1);
            yield return new OverlapCase($"size {size}, X inside and Y above", size, CurrentX, CurrentY + size);
            yield return new OverlapCase($"size {size}, Y inside and X west", size, CurrentX - 1, CurrentY);
            yield return new OverlapCase($"size {size}, Y inside and X east", size, CurrentX + size, CurrentY);
        }
    }

    private static IEnumerable<DoorInteractionCase> CreateLargeDoorInteractionCases()
    {
        foreach (var size in new[] { 2, 3 })
        {
            yield return DoorCase("shape 0 rotation 0 south", 0, 0, size, CurrentX, CurrentY - 1, CurrentX, CurrentY);
            yield return DoorCase("shape 0 rotation 1 east", 0, 1, size, CurrentX + size, CurrentY, CurrentX + size - 1, CurrentY);
            yield return DoorCase("shape 0 rotation 2 south", 0, 2, size, CurrentX, CurrentY - 1, CurrentX, CurrentY);
            yield return DoorCase("shape 0 rotation 3 east", 0, 3, size, CurrentX + size, CurrentY, CurrentX + size - 1, CurrentY);

            yield return DoorCase("shape 2 rotation 0 west", 2, 0, size, CurrentX - 1, CurrentY, CurrentX, CurrentY);
            yield return DoorCase("shape 2 rotation 1 east", 2, 1, size, CurrentX + size, CurrentY, CurrentX + size - 1, CurrentY);
            yield return DoorCase("shape 2 rotation 2 east", 2, 2, size, CurrentX + size, CurrentY, CurrentX + size - 1, CurrentY);
            yield return DoorCase("shape 2 rotation 3 south", 2, 3, size, CurrentX, CurrentY - 1, CurrentX, CurrentY);

            yield return DoorCase("shape 9 rotation 0 south", 9, 0, size, CurrentX, CurrentY - 1, CurrentX, CurrentY);
            yield return DoorCase("shape 9 rotation 1 north", 9, 1, size, CurrentX, CurrentY + size, CurrentX, CurrentY + size - 1);
            yield return DoorCase("shape 9 rotation 2 east", 9, 2, size, CurrentX + size, CurrentY, CurrentX + size - 1, CurrentY);
            yield return DoorCase("shape 9 rotation 3 west", 9, 3, size, CurrentX - 1, CurrentY, CurrentX, CurrentY);
        }
    }

    private static IEnumerable<DecorationInteractionCase> CreateLargeDecorationInteractionCases()
    {
        foreach (var size in new[] { 2, 3 })
        {
            foreach (var shape in new[] { 6, 7 })
            {
                yield return DecorationCase("rotation 0 west", shape, RotationFor(shape, 0), size, CurrentX - 1, CurrentY, CurrentX, CurrentY, CollisionFlag.WallWest);
                yield return DecorationCase("rotation 0 north", shape, RotationFor(shape, 0), size, CurrentX, CurrentY + size, CurrentX, CurrentY + size - 1, CollisionFlag.WallNorth);
                yield return DecorationCase("rotation 1 east", shape, RotationFor(shape, 1), size, CurrentX + size, CurrentY, CurrentX + size - 1, CurrentY, CollisionFlag.WallEast);
                yield return DecorationCase("rotation 1 north", shape, RotationFor(shape, 1), size, CurrentX, CurrentY + size, CurrentX, CurrentY + size - 1, CollisionFlag.WallNorth);
                yield return DecorationCase("rotation 2 east", shape, RotationFor(shape, 2), size, CurrentX + size, CurrentY, CurrentX + size - 1, CurrentY, CollisionFlag.WallEast);
                yield return DecorationCase("rotation 2 south", shape, RotationFor(shape, 2), size, CurrentX, CurrentY - 1, CurrentX, CurrentY, CollisionFlag.WallSouth);
                yield return DecorationCase("rotation 3 west", shape, RotationFor(shape, 3), size, CurrentX - 1, CurrentY, CurrentX, CurrentY, CollisionFlag.WallWest);
                yield return DecorationCase("rotation 3 south", shape, RotationFor(shape, 3), size, CurrentX, CurrentY - 1, CurrentX, CurrentY, CollisionFlag.WallSouth);
            }

            yield return DecorationCase("shape 8 south", 8, 0, size, CurrentX, CurrentY - 1, CurrentX, CurrentY, CollisionFlag.WallSouth);
            yield return DecorationCase("shape 8 north", 8, 1, size, CurrentX, CurrentY + size, CurrentX, CurrentY + size - 1, CollisionFlag.WallNorth);
            yield return DecorationCase("shape 8 east", 8, 2, size, CurrentX + size, CurrentY, CurrentX + size - 1, CurrentY, CollisionFlag.WallEast);
            yield return DecorationCase("shape 8 west", 8, 3, size, CurrentX - 1, CurrentY, CurrentX, CurrentY, CollisionFlag.WallWest);
        }
    }

    private static DoorInteractionCase DoorCase(string name, int shape, int rotation, int size, int targetX, int targetY, int collisionX, int collisionY) =>
        new($"size {size}, {name}", shape, rotation, size, targetX, targetY, collisionX, collisionY);

    private static DecorationInteractionCase DecorationCase(
        string name,
        int shape,
        int rotation,
        int size,
        int targetX,
        int targetY,
        int collisionX,
        int collisionY,
        CollisionFlag blockingFlag) =>
        new($"size {size}, shape {shape}, {name}", shape, rotation, size, targetX, targetY, collisionX, collisionY, blockingFlag);

    private static int RotationFor(int shape, int effectiveRotation) => shape == 7 ? effectiveRotation + 2 & 0x3 : effectiveRotation;

    public sealed record OverlapCase(string Name, int Size, int TargetX, int TargetY);

    public sealed record DoorInteractionCase(
        string Name,
        int Shape,
        int Rotation,
        int Size,
        int TargetX,
        int TargetY,
        int CollisionX,
        int CollisionY);

    public sealed record DecorationInteractionCase(
        string Name,
        int Shape,
        int Rotation,
        int Size,
        int TargetX,
        int TargetY,
        int CollisionX,
        int CollisionY,
        CollisionFlag BlockingFlag);
}
