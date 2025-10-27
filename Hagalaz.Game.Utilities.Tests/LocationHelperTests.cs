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
        [DataRow(1, 0, 0, 1)]
        [DataRow(0, 1, 0, 64)]
        [DataRow(0, 0, 1, 4096)]
        [DataRow(31, 31, 1, 6111)]
        [DataRow(32, 32, 2, 10272)]
        [DataRow(62, 62, 2, 12222)]
        [DataRow(15, 47, 3, 15311)]
        [DataRow(47, 15, 1, 5087)]
        [DataRow(0, 63, 0, 4032)]
        [DataRow(63, 0, 0, 63)]
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
        [DataRow(1, 0, 0, 1)]
        [DataRow(0, 1, 0, 1024)]
        [DataRow(0, 0, 1, 2097152)]
        [DataRow(511, 1023, 1, 3144191)]
        [DataRow(512, 1024, 2, 5243392)]
        [DataRow(1022, 2046, 2, 6290430)]
        [DataRow(255, 1279, 3, 7601407)]
        [DataRow(1279, 255, 1, 2358271)]
        [DataRow(0, 2047, 0, 2096128)]
        [DataRow(1023, 0, 0, 1023)]
        public void GetRegionPartHash_ReturnsCorrectHash(int partX, int partY, int z, int expectedHash)
        {
            var result = LocationHelper.GetRegionPartHash(partX, partY, z);
            Assert.AreEqual(expectedHash, result, "The calculated region part hash is incorrect.");
        }
    }
}