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
        [DataRow(-10, 5, 8, 30)]
        [DataRow(10, -5, 8, -30)]
        [DataRow(10, 5, -8, -30)]
        [DataRow(-10, -5, -8, 30)]
        public void ConvertLocalToAbsolute_ReturnsCorrectCoordinate(int local, int grid, int size, int expected)
        {
            var result = LocationHelper.ConvertLocalToAbsolute(local, grid, size);
            Assert.AreEqual(expected, result, "The absolute coordinate is incorrect.");
        }

        [DataTestMethod]
        [DataRow(10, 20, 1, 5386)]
        [DataRow(0, 0, 0, 0)]
        [DataRow(63, 63, 3, 16383)]
        [DataRow(-1, -1, -1, -1)]
        [DataRow(63, 20, 1, 5439)]
        [DataRow(10, 63, 1, 8138)]
        [DataRow(10, 20, 3, 13578)]
        [DataRow(63, 63, 1, 8191)]
        public void GetRegionLocalHash_ReturnsCorrectHash(int x, int y, int z, int expectedHash)
        {
            var result = LocationHelper.GetRegionLocalHash(x, y, z);
            Assert.AreEqual(expectedHash, result, "The calculated region local hash is incorrect.");
        }

        [DataTestMethod]
        [DataRow(10, 20, 1, 2117642)]
        [DataRow(0, 0, 0, 0)]
        [DataRow(1023, 2047, 3, 8388607)]
        [DataRow(-1, -1, -1, 8388607)]
        [DataRow(1023, 20, 1, 2118655)]
        [DataRow(10, 2047, 1, 4193290)]
        [DataRow(10, 20, 3, 6311946)]
        [DataRow(1023, 2047, 1, 4194303)]
        public void GetRegionPartHash_ReturnsCorrectHash(int partX, int partY, int z, int expectedHash)
        {
            var result = LocationHelper.GetRegionPartHash(partX, partY, z);
            Assert.AreEqual(expectedHash, result, "The calculated region part hash is incorrect.");
        }
    }
}