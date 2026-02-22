using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Utilities.Tests
{
    [TestClass]
    public class GameObjectHelperTests
    {
        [TestMethod]
        [DataRow(10, 20, 1, 5, 169226)]
        [DataRow(0, 0, 0, 0, 0)]
        [DataRow(63, 63, 3, 31, 1032191)]
        [DataRow(-1, -1, -1, -1, -1)]
        [DataRow(1, 0, 0, 0, 1)]
        [DataRow(0, 1, 0, 0, 64)]
        [DataRow(0, 0, 1, 0, 4096)]
        [DataRow(0, 0, 0, 1, 32768)]
        public void GetRegionLocalHash_ReturnsCorrectHash(int x, int y, int z, int layer, int expectedHash)
        {
            var result = GameObjectHelper.GetRegionLocalHash(x, y, z, layer);
            Assert.AreEqual(expectedHash, result, "The calculated region local hash is incorrect.");
        }
    }
}
