using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Utilities.Tests
{
    [TestClass]
    public class CreatureHelperTests
    {
        [TestMethod]
        public void CalculatePredictedDamage_WithNoHits_ReturnsMinusOne()
        {
            var hits = new int[] { };
            var result = CreatureHelper.CalculatePredictedDamage(hits);
            Assert.AreEqual(-1, result, "Should return -1 for no hits.");
        }

        [TestMethod]
        public void CalculatePredictedDamage_WithSingleHit_ReturnsHitValue()
        {
            var hits = new int[] { 10 };
            // This test will fail due to the bug in the implementation.
            // Expected: 10, Actual: 0
            Assert.AreEqual(10, CreatureHelper.CalculatePredictedDamage(hits), "Should return the value of the single hit.");
        }

        [TestMethod]
        public void CalculatePredictedDamage_WithMultipleHits_ReturnsSumOfHits()
        {
            var hits = new int[] { 10, 20, 5 };
            // This test will fail due to the bug in the implementation.
            // Expected: 35, Actual: 3 (0 + 1 + 2)
            Assert.AreEqual(35, CreatureHelper.CalculatePredictedDamage(hits), "Should return the sum of all hits.");
        }

        [TestMethod]
        public void CalculatePredictedDamage_WithMisses_IgnoresMissesAndReturnsSumOfHits()
        {
            // Assuming -1 represents a miss
            var hits = new int[] { 10, -1, 20, -1, 5 };
            // This test will fail due to the bug in the implementation.
            // Expected: 35, Actual: 10 (0+1+2+3+4)
            Assert.AreEqual(35, CreatureHelper.CalculatePredictedDamage(hits), "Should ignore misses and return the sum of hits.");
        }

        [TestMethod]
        public void CalculatePredictedDamage_WithAllMisses_ReturnsMinusOne()
        {
            var hits = new int[] { -1, -1, -1 };
            // This test will fail due to the bug in the implementation.
            // Expected: -1, Actual: 3 (0+1+2)
            Assert.AreEqual(-1, CreatureHelper.CalculatePredictedDamage(hits), "Should return -1 if all hits are misses.");
        }

        [DataTestMethod]
        [DataRow(0, 0)]
        [DataRow(1, 1)]
        [DataRow(29, 1)]
        [DataRow(30, 1)]
        [DataRow(31, 2)]
        [DataRow(60, 2)]
        [DataRow(600, 20)]
        public void CalculateTicksForClientTicks_ReturnsCorrectValue(int clientTicks, int expectedServerTicks)
        {
            var result = CreatureHelper.CalculateTicksForClientTicks(clientTicks);
            Assert.AreEqual(expectedServerTicks, result, $"For {clientTicks} client ticks, expected {expectedServerTicks} server ticks.");
        }
    }
}