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
            var vector = Substitute.For<IVector3>();
            vector.X.Returns(1);
            vector.Y.Returns(2);
            vector.Z.Returns(3);

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
            var location = Substitute.For<ILocation>();
            location.X.Returns(10);
            location.Y.Returns(20);
            location.Z.Returns(30);

            // Act
            var newLocation = location.Translate(1, 2, 3);

            // Assert
            Assert.AreEqual(11, newLocation.X);
            Assert.AreEqual(22, newLocation.Y);
            Assert.AreEqual(33, newCastedLocation.Z);
        }

        [TestMethod]
        public void GetRegionPartHash_ReturnsCorrectHash()
        {
            // Arrange
            var location = Substitute.For<ILocation>();
            location.X.Returns(3200);
            location.Y.Returns(3200);
            location.Z.Returns(0);
            var expectedHash = LocationHelper.GetRegionPartHash(location.X, location.Y, location.Z);

            // Act
            var hash = location.GetRegionPartHash();

            // Assert
            Assert.AreEqual(expectedHash, hash);
        }

        [TestMethod]
        public void GetRegionLocalHash_ReturnsCorrectHash()
        {
            // Arrange
            var location = Substitute.For<ILocation>();
            location.X.Returns(3205);
            location.Y.Returns(3205);
            location.Z.Returns(1);
            var expectedHash = LocationHelper.GetRegionLocalHash(location.X, location.Y);


            // Act
            var hash = location.GetRegionLocalHash();

            // Assert
            Assert.AreEqual(expectedHash, hash);
        }
    }
}
