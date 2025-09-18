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
        public void ToLocation_ConvertsVector3ToLocation()
        {
            // Arrange
            var vector = Substitute.For<IVector3>();
            vector.X.Returns(10);
            vector.Y.Returns(20);
            vector.Z.Returns(1);

            // Act
            var location = vector.ToLocation();

            // Assert
            Assert.AreEqual(10, location.X);
            Assert.AreEqual(20, location.Y);
            Assert.AreEqual(1, location.Z);
        }

        [TestMethod]
        public void Translate_WithDirection_ReturnsTranslatedLocation()
        {
            // Arrange
            var location = Substitute.For<ILocation>();
            location.X.Returns(10);
            location.Y.Returns(20);
            location.Z.Returns(1);
            location.Translate(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(x => Location.Create(location.X + (int)x[0], location.Y + (int)x[1], location.Z + (int)x[2]));

            // Act
            var newLocation = location.Translate(DirectionFlag.North);

            // Assert
            Assert.AreEqual(10, newLocation.X);
            Assert.AreEqual(21, newLocation.Y);
            Assert.AreEqual(1, newLocation.Z);
        }

        [TestMethod]
        public void GetRegionPartHash_ReturnsCorrectHash()
        {
            // Arrange
            var location = Substitute.For<ILocation>();
            location.RegionPartX.Returns(1);
            location.RegionPartY.Returns(2);
            location.Z.Returns(3);

            // Act
            var hash = location.GetRegionPartHash();

            // Assert
            var expectedHash = LocationHelper.GetRegionPartHash(1, 2, 3);
            Assert.AreEqual(expectedHash, hash);
        }

        [TestMethod]
        public void GetRegionLocalHash_ReturnsCorrectHash()
        {
            // Arrange
            var location = Substitute.For<ILocation>();
            location.X.Returns(10);
            location.Y.Returns(20);
            location.Z.Returns(1);

            // Act
            var hash = location.GetRegionLocalHash();

            // Assert
            var expectedHash = LocationHelper.GetRegionLocalHash(10, 20, 1);
            Assert.AreEqual(expectedHash, hash);
        }
    }
}
