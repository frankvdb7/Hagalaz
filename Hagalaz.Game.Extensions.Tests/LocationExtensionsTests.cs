using Hagalaz.Game.Abstractions;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Extensions.Tests
{
    [TestClass]
    public class LocationExtensionsTests
    {
        [TestMethod]
        public void ToLocation_WithVector3_ReturnsCorrectLocation()
        {
            // Arrange
            var vector = new Location(1, 2, 3, 0);

            // Act
            var location = vector.ToLocation();

            // Assert
            Assert.AreEqual(1, location.X);
            Assert.AreEqual(2, location.Y);
            Assert.AreEqual(3, location.Z);
        }

        [TestMethod]
        public void Translate_WithOffsets_ReturnsCorrectLocation()
        {
            // Arrange
            var location = new Location(10, 20, 30, 0);

            // Act
            var newLocation = location.Translate(1, 2, 3);

            // Assert
            Assert.AreEqual(11, newLocation.X);
            Assert.AreEqual(22, newLocation.Y);
            Assert.AreEqual(33, newLocation.Z);
        }

        [TestMethod]
        public void GetRegionPartHash_ReturnsCorrectHash()
        {
            // Arrange
            var location = new Location(3200, 3200, 0, 0);
            var expectedHash = LocationHelper.GetRegionPartHash(location.RegionPartX, location.RegionPartY, location.Z);

            // Act
            var hash = location.GetRegionPartHash();

            // Assert
            Assert.AreEqual(expectedHash, hash);
        }

        [TestMethod]
        public void GetRegionLocalHash_ReturnsCorrectHash()
        {
            // Arrange
            var location = new Location(3205, 3205, 1, 0);
            var expectedHash = LocationHelper.GetRegionLocalHash(location.X, location.Y, location.Z);


            // Act
            var hash = location.GetRegionLocalHash();

            // Assert
            Assert.AreEqual(expectedHash, hash);
        }
    }
}
