using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using NSubstitute;
using Path = Hagalaz.Services.GameWorld.Logic.Pathfinding.Path;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class PathfinderTests
    {
        private DumbPathFinder _pathfinder;
        private IMapRegionService _mapRegionService;

        [TestInitialize]
        public void Initialize()
        {
            _mapRegionService = Substitute.For<IMapRegionService>();
            _pathfinder = new DumbPathFinder(_mapRegionService);
        }

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
            _mapRegionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(CollisionFlag.TraversableEastBlocked);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsFalse(path.Successful);
        }

        [TestMethod]
        public void Find_LargeEntity_UsesVariableTraversal()
        {
            // Arrange
            var from = Location.Create(1, 1, 0);
            var to = Location.Create(5, 5, 0);
            var selfSize = 3;

            // Act
            var path = _pathfinder.Find(from, selfSize, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsTrue(path.Successful);
            Assert.IsTrue(path.Any());
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
    }
}