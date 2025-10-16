using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Utilities.Tests
{
    [TestClass]
    public class LocationHelperTests
    {
        [DataTestMethod]
        [DataRow(10, 5, 8, 50)]
        [DataRow(0, 0, 10, 0)]
        [DataRow(5, 10, 10, 105)]
        [DataRow(0, 0, 0, 0)]
        public void ConvertLocalToAbsolute_ReturnsCorrectCoordinate(int local, int grid, int size, int expected)
        {
            var result = LocationHelper.ConvertLocalToAbsolute(local, grid, size);
            Assert.AreEqual(expected, result, "The absolute coordinate is incorrect.");
        }

        [DataTestMethod]
        [DataRow(10, 20, 1, 5386)]
        [DataRow(0, 0, 0, 0)]
        [DataRow(63, 63, 3, 16383)]
        public void GetRegionLocalHash_ReturnsCorrectHash(int x, int y, int z, int expectedHash)
        {
            var result = LocationHelper.GetRegionLocalHash(x, y, z);
            Assert.AreEqual(expectedHash, result, "The calculated region local hash is incorrect.");
        }

        [DataTestMethod]
        [DataRow(10, 20, 1, 2117642)]
        [DataRow(0, 0, 0, 0)]
        [DataRow(1023, 2047, 3, 8388607)]
        public void GetRegionPartHash_ReturnsCorrectHash(int partX, int partY, int z, int expectedHash)
        {
            var result = LocationHelper.GetRegionPartHash(partX, partY, z);
            Assert.AreEqual(expectedHash, result, "The calculated region part hash is incorrect.");
        }
    }
}