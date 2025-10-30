using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Logic.Pathfinding;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests
{
    [TestClass]
    public class ProjectilePathfinderTests
    {
        private ProjectilePathFinder _pathfinder;
        private IMapRegionService _mapRegionService;

        [TestInitialize]
        public void Initialize()
        {
            _mapRegionService = Substitute.For<IMapRegionService>();
            _mapRegionService.GetClippingFlag(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns(CollisionFlag.Walkable);
            _pathfinder = new ProjectilePathFinder(_mapRegionService);
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
            Assert.AreEqual(to, path.Last());
        }

        [TestMethod]
        public void Find_PathBlockedByObjectAllowingRange_ReturnsSuccessfulPath()
        {
            // Arrange
            var from = Location.Create(1, 1, 0);
            var to = Location.Create(3, 1, 0);

            // Block the path with an object that allows ranged attacks over it
            _mapRegionService.GetClippingFlag(2, 1, 0).Returns(CollisionFlag.ObjectBlock | CollisionFlag.ObjectAllowRange);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsTrue(path.Successful);
            Assert.IsTrue(path.Any());
            Assert.AreEqual(to, path.Last());
        }

        [TestMethod]
        public void Find_PathBlockedByFloor_ReturnsUnsuccessfulPath()
        {
            // Arrange
            var from = Location.Create(1, 1, 0);
            var to = Location.Create(3, 1, 0);

            // Block the path with a solid wall
            _mapRegionService.GetClippingFlag(2, 1, 0).Returns(CollisionFlag.FloorBlock);

            // Act
            var path = _pathfinder.Find(from, 1, to, 1, 1, 0, 0, 0, false);

            // Assert
            Assert.IsFalse(path.Successful);
        }
    }
}
