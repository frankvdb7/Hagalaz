using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using NSubstitute;
using Path = Hagalaz.Services.GameWorld.Logic.Pathfinding.Path;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class DumbPathfinderTests
    {
        private DumbPathFinder _pathfinder;
        private IMapRegionService _mapRegionService;

        [TestInitialize]
        public void Initialize()
        {
            _mapRegionService = Substitute.For<IMapRegionService>();
            _mapRegionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(CollisionFlag.Walkable);
            _pathfinder = new DumbPathFinder(_mapRegionService);
        }

        public static IEnumerable<TestDataRow<TraversalCase>> SingleTraversalCases =>
            CreateSingleTraversalCases()
                .Select(testCase => new TestDataRow<TraversalCase>(testCase) { DisplayName = testCase.Name });

        public static IEnumerable<TestDataRow<SizeTwoTraversalCase>> SizeTwoTraversalCases =>
            CreateSizeTwoTraversalCases()
                .Select(testCase => new TestDataRow<SizeTwoTraversalCase>(testCase) { DisplayName = testCase.Name });

        [TestMethod]
        public void Find_SameStartAndEnd_ReturnsSuccessfulPath()
        {
            // Arrange
            var from = Location.Create(5, 5, 0);
            var to = Location.Create(5, 5, 0);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsTrue(path.Successful);
            Assert.AreEqual(0, path.Count());
        }

        [TestMethod]
        public void Find_SimplePath_NoObstacles_ReturnsSuccessfulPath()
        {
            // Arrange
            var from = Location.Create(1, 1, 0);
            var to = Location.Create(3, 1, 0);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsTrue(path.Successful);
            Assert.IsTrue(path.Any());
            Assert.AreEqual(to, path.Last()); // Last step should be the target
        }

        [TestMethod]
        public void Find_PathBlocked_ReturnsUnsuccessfulPath()
        {
            // Arrange
            var from = Location.Create(1, 1, 0);
            var to = Location.Create(3, 1, 0);

            // Mock getClippingFlag
            _mapRegionService.GetClippingFlag(2, 1, 0).Returns(CollisionFlag.FloorBlock);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsFalse(path.Successful);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void Find_PathForDifferentSizes_ReturnsSuccessfulPath(int size)
        {
            // Arrange
            var from = Location.Create(1, 1, 0);
            var to = Location.Create(5, 5, 0);

            // Act
            var path = _pathfinder.Find(from, size, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsTrue(path.Successful);
            Assert.IsTrue(path.Any());
            Assert.AreEqual(to, path.Last());
        }

        [TestMethod]
        public void DumbPathfinder_Find_BlockedPath_MoveNear_ReturnsPartialPath()
        {
            // Arrange
            var from = Location.Create(1, 1, 0);
            var to = Location.Create(5, 1, 0);

            // Block a tile on the path
            _mapRegionService.GetClippingFlag(3, 1, 0).Returns(CollisionFlag.FloorBlock);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, true);

            // Assert
            Assert.IsFalse(path.Successful);
            Assert.IsTrue(path.MovedNear);
            Assert.AreEqual(Location.Create(2, 1, 0), path.LastOrDefault());
        }

        [TestMethod]
        public void Find_SimpleDiagonalPath_ReturnsSuccessfulPath()
        {
            // Arrange
            var from = Location.Create(1, 1, 0);
            var to = Location.Create(3, 3, 0);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsTrue(path.Successful);
            Assert.IsTrue(path.Any());
            Assert.AreEqual(to, path.Last());
        }

        [TestMethod]
        public void CheckSingleTraversal_ValidMove_UpdatesCoordinates()
        {
            // Arrange
            var path = new Path();
            int x = 5, y = 5, z = 0;
            var direction = DirectionFlag.North;

            // Act
            _pathfinder.CheckSingleTraversal(path, direction, ref x, ref y, ref z);

            // Assert
            Assert.AreEqual(6, y); // Moved north
            Assert.IsTrue(path.Any());
        }

        [TestMethod]
        [DynamicData(nameof(SingleTraversalCases))]
        public void Find_SizeOneClearDirection_ReachesRequestedNeighbor(TraversalCase testCase)
        {
            // Arrange
            var from = Location.Create(10, 10, 0);
            var to = Location.Create(from.X + testCase.DeltaX, from.Y + testCase.DeltaY, from.Z);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsTrue(path.Successful, testCase.Name);
            Assert.AreEqual(1, path.Count(), testCase.Name);
            Assert.AreEqual(to, path.Single(), testCase.Name);
        }

        [TestMethod]
        [DynamicData(nameof(SingleTraversalCases))]
        public void Find_SizeOneMatchingDirectionalBlocker_ReturnsUnsuccessfulPath(TraversalCase testCase)
        {
            // Arrange
            var from = Location.Create(10, 10, 0);
            var to = Location.Create(from.X + testCase.DeltaX, from.Y + testCase.DeltaY, from.Z);
            _mapRegionService.GetClippingFlag(to.X, to.Y, to.Z).Returns(testCase.MatchingBlocker);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsFalse(path.Successful, testCase.Name);
            Assert.AreEqual(0, path.Count(), testCase.Name);
        }

        [TestMethod]
        [DynamicData(nameof(SingleTraversalCases))]
        public void Find_SizeOneUnrelatedDirectionalBlocker_ReachesRequestedNeighbor(TraversalCase testCase)
        {
            // Arrange
            var from = Location.Create(10, 10, 0);
            var to = Location.Create(from.X + testCase.DeltaX, from.Y + testCase.DeltaY, from.Z);
            _mapRegionService.GetClippingFlag(to.X, to.Y, to.Z).Returns(testCase.UnrelatedBlocker);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsTrue(path.Successful, testCase.Name);
            Assert.AreEqual(1, path.Count(), testCase.Name);
            Assert.AreEqual(to, path.Single(), testCase.Name);
        }

        [TestMethod]
        [DynamicData(nameof(SizeTwoTraversalCases))]
        public void Find_SizeTwoClearDirection_ReachesRequestedNeighbor(SizeTwoTraversalCase testCase)
        {
            // Arrange
            var from = Location.Create(10, 10, 0);
            var to = Location.Create(from.X + testCase.DeltaX, from.Y + testCase.DeltaY, from.Z);

            // Act
            var path = _pathfinder.Find(from, 2, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsTrue(path.Successful, testCase.Name);
            Assert.AreEqual(1, path.Count(), testCase.Name);
            Assert.AreEqual(to, path.Single(), testCase.Name);
        }

        [TestMethod]
        [DynamicData(nameof(SizeTwoTraversalCases))]
        public void Find_SizeTwoNewlyOccupiedFootprintBlocker_ReturnsUnsuccessfulPath(SizeTwoTraversalCase testCase)
        {
            // Arrange
            var from = Location.Create(10, 10, 0);
            var to = Location.Create(from.X + testCase.DeltaX, from.Y + testCase.DeltaY, from.Z);
            _mapRegionService.GetClippingFlag(
                    from.X + testCase.CollisionDeltaX,
                    from.Y + testCase.CollisionDeltaY,
                    from.Z)
                .Returns(testCase.BlockingFlag);

            // Act
            var path = _pathfinder.Find(from, 2, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsFalse(path.Successful, testCase.Name);
            Assert.AreEqual(0, path.Count(), testCase.Name);
        }

        [TestMethod]
        [DataRow(3)]
        [DataRow(4)]
        public void Find_SizeThreeOrLargerSouthWest_ReachesRequestedNeighbor(int size)
        {
            // Arrange
            var from = Location.Create(10, 10, 0);
            var to = Location.Create(9, 9, 0);

            // Act
            var path = _pathfinder.Find(from, size, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsTrue(path.Successful);
            Assert.AreEqual(1, path.Count());
            Assert.AreEqual(to, path.Single());
        }

        private static IEnumerable<TraversalCase> CreateSingleTraversalCases()
        {
            yield return new TraversalCase("north", 0, 1, CollisionFlag.WallAllowRangeSouth, CollisionFlag.WallAllowRangeNorth);
            yield return new TraversalCase("north-east", 1, 1, CollisionFlag.WallAllowRangeNorthEast, CollisionFlag.WallAllowRangeNorthWest);
            yield return new TraversalCase("east", 1, 0, CollisionFlag.WallAllowRangeWest, CollisionFlag.WallAllowRangeEast);
            yield return new TraversalCase("south-east", 1, -1, CollisionFlag.WallAllowRangeSouthEast, CollisionFlag.WallAllowRangeSouthWest);
            yield return new TraversalCase("south", 0, -1, CollisionFlag.WallAllowRangeNorth, CollisionFlag.WallAllowRangeSouth);
            yield return new TraversalCase("south-west", -1, -1, CollisionFlag.WallAllowRangeSouthWest, CollisionFlag.WallAllowRangeSouthEast);
            yield return new TraversalCase("west", -1, 0, CollisionFlag.WallAllowRangeEast, CollisionFlag.WallAllowRangeWest);
            yield return new TraversalCase("north-west", -1, 1, CollisionFlag.WallAllowRangeNorthWest, CollisionFlag.WallAllowRangeNorthEast);
        }

        private static IEnumerable<SizeTwoTraversalCase> CreateSizeTwoTraversalCases()
        {
            yield return new SizeTwoTraversalCase("north", 0, 1, 0, 2, CollisionFlag.WallAllowRangeNorthWest);
            yield return new SizeTwoTraversalCase("north-east", 1, 1, 1, 2, CollisionFlag.WallAllowRangeNorthWest);
            yield return new SizeTwoTraversalCase("east", 1, 0, 2, 0, CollisionFlag.WallAllowRangeSouthEast);
            yield return new SizeTwoTraversalCase("south-east", 1, -1, 1, -1, CollisionFlag.WallAllowRangeSouthWest);
            yield return new SizeTwoTraversalCase("south", 0, -1, 0, -1, CollisionFlag.WallAllowRangeSouthWest);
            yield return new SizeTwoTraversalCase("south-west", -1, -1, -1, -1, CollisionFlag.WallAllowRangeSouthWest);
            yield return new SizeTwoTraversalCase("west", -1, 0, -1, 0, CollisionFlag.WallAllowRangeSouthWest);
            yield return new SizeTwoTraversalCase("north-west", -1, 1, -1, 1, CollisionFlag.WallAllowRangeSouthWest);
        }

        public sealed record TraversalCase(
            string Name,
            int DeltaX,
            int DeltaY,
            CollisionFlag MatchingBlocker,
            CollisionFlag UnrelatedBlocker);

        public sealed record SizeTwoTraversalCase(
            string Name,
            int DeltaX,
            int DeltaY,
            int CollisionDeltaX,
            int CollisionDeltaY,
            CollisionFlag BlockingFlag);
    }
}
