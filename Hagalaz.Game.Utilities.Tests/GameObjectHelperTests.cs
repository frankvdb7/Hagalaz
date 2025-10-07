using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Utilities.Tests
{
    [TestClass]
    public class GameObjectHelperTests
    {
        [DataTestMethod]
        [DataRow(10, 20, 1, 0, 5386)]
        [DataRow(0, 0, 0, 0, 0)]
        [DataRow(63, 63, 3, 3, 114687)]
        [DataRow(32, 16, 2, 1, 42016)]
        public void GetRegionLocalHash_ReturnsCorrectHash(int x, int y, int z, int layer, int expectedHash)
        {
            var result = GameObjectHelper.GetRegionLocalHash(x, y, z, layer);
            Assert.AreEqual(expectedHash, result, "The calculated hash is incorrect.");
        }
    }
}