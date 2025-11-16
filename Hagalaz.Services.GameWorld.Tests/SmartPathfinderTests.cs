using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class SmartPathfinderTests
    {
        private SmartPathFinder _pathfinder;
        private IMapRegionService _mapRegionService;

        [TestInitialize]
        public void Initialize()
        {
            _mapRegionService = Substitute.For<IMapRegionService>();
            _mapRegionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns(CollisionFlag.Walkable);
            _pathfinder = new SmartPathFinder(_mapRegionService);
        }

        [TestMethod]
        public void Find_SimplePath_ReturnsSuccessfulPath()
        {
            // Arrange
            var from = Location.Create(1, 1, 0);
            var to = Location.Create(3, 1, 0);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsTrue(path.Successful);
            Assert.IsTrue(path.Any());
            var lastPoint = path.Last();
            Assert.IsTrue(Math.Abs(to.X - lastPoint.X) <= 1 && Math.Abs(to.Y - lastPoint.Y) <= 1);
        }

        [TestMethod]
        public void Find_PathBlocked_FindsAlternative()
        {
            // Arrange
            var from = Location.Create(1, 1, 0);
            var to = Location.Create(3, 1, 0);

            // Block the direct path
            _mapRegionService.GetClippingFlag(2, 1, 0).Returns(CollisionFlag.FloorBlock);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsTrue(path.Successful);
            Assert.IsTrue(path.Any());
            var lastPoint = path.Last();
            Assert.IsTrue(Math.Abs(to.X - lastPoint.X) <= 1 && Math.Abs(to.Y - lastPoint.Y) <= 1);
            Assert.AreNotEqual(Location.Create(2, 1, 0), path.First()); // Ensure it didn't go through the obstacle
        }

        [TestMethod]
        public void Find_UnreachableDestination_ReturnsUnsuccessfulPath()
        {
            // Arrange
            var from = Location.Create(1, 1, 0);
            var to = Location.Create(3, 1, 0);

            // Block all paths to the destination
            _mapRegionService.GetClippingFlag(2, 0, 0).Returns(CollisionFlag.FloorBlock);
            _mapRegionService.GetClippingFlag(2, 1, 0).Returns(CollisionFlag.FloorBlock);
            _mapRegionService.GetClippingFlag(2, 2, 0).Returns(CollisionFlag.FloorBlock);
            _mapRegionService.GetClippingFlag(3, 0, 0).Returns(CollisionFlag.FloorBlock);
            _mapRegionService.GetClippingFlag(3, 2, 0).Returns(CollisionFlag.FloorBlock);
            _mapRegionService.GetClippingFlag(4, 0, 0).Returns(CollisionFlag.FloorBlock);
            _mapRegionService.GetClippingFlag(4, 1, 0).Returns(CollisionFlag.FloorBlock);
            _mapRegionService.GetClippingFlag(4, 2, 0).Returns(CollisionFlag.FloorBlock);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsFalse(path.Successful);
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
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
            var lastPoint = path.Last();
            Assert.IsTrue(Math.Abs(to.X - lastPoint.X) <= size && Math.Abs(to.Y - lastPoint.Y) <= size);
        }

    }
}
