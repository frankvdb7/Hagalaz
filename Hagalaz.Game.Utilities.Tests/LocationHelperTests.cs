using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Game.Utilities.Tests
{
    [TestClass]
    public class LocationHelperTests
    {
        [DataTestMethod]
        [DataRow(10, 20, 1, 5386)]
        [DataRow(0, 0, 0, 0)]
        [DataRow(63, 63, 3, 16383)]
        [DataRow(30, 50, 2, 11422)]
        public void GetRegionLocalHash_WithVariousCoordinates_ReturnsCorrectHash(int x, int y, int z, int expected)
        {
            // Act
            var result = LocationHelper.GetRegionLocalHash(x, y, z);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [DataTestMethod]
        [DataRow(10, 20, 1, 2117642)]
        [DataRow(0, 0, 0, 0)]
        [DataRow(1023, 2047, 3, 8388607)]
        [DataRow(500, 1000, 2, 5218804)]
        public void GetRegionPartHash_WithVariousCoordinates_ReturnsCorrectHash(int partX, int partY, int z, int expected)
        {
            // Act
            var result = LocationHelper.GetRegionPartHash(partX, partY, z);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [DataTestMethod]
        [DataRow(10, 2, 8, 26)]
        [DataRow(0, 0, 10, 0)]
        [DataRow(5, 3, 16, 53)]
        public void ConvertLocalToAbsolute_WithVariousCoordinates_ReturnsCorrectAbsoluteCoordinate(int local, int grid, int size, int expected)
        {
            // Act
            var result = LocationHelper.ConvertLocalToAbsolute(local, grid, size);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}