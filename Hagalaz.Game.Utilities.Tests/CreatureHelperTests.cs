using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Game.Utilities.Tests
{
    [TestClass]
    public class CreatureHelperTests
    {
        [TestMethod]
        public void CalculatePredictedDamage_WithPositiveHits_ReturnsSum()
        {
            // Arrange
            var hits = new[] { 10, 20, 5 };

            // Act
            var result = CreatureHelper.CalculatePredictedDamage(hits);

            // Assert
            Assert.AreEqual(35, result);
        }

        [TestMethod]
        public void CalculatePredictedDamage_WithMixedHits_ReturnsSumOfPositiveHits()
        {
            // Arrange
            var hits = new[] { 10, -5, 20, 0, 5 };

            // Act
            var result = CreatureHelper.CalculatePredictedDamage(hits);

            // Assert
            Assert.AreEqual(35, result);
        }

        [TestMethod]
        public void CalculatePredictedDamage_WithOnlyNegativeAndZeroHits_ReturnsMinusOne()
        {
            // Arrange
            var hits = new[] { -10, -5, 0 };

            // Act
            var result = CreatureHelper.CalculatePredictedDamage(hits);

            // Assert
            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void CalculatePredictedDamage_WithNullValueInArray_ReturnsSumOfPositiveHits()
        {
            // Arrange
            var hits = new[] { 10, 0, -5, 20, 5 };

            // Act
            var result = CreatureHelper.CalculatePredictedDamage(hits);

            // Assert
            Assert.AreEqual(35, result);
        }

        [TestMethod]
        public void CalculatePredictedDamage_WithEmptyArray_ReturnsMinusOne()
        {
            // Arrange
            var hits = new int[0];

            // Act
            var result = CreatureHelper.CalculatePredictedDamage(hits);

            // Assert
            Assert.AreEqual(-1, result);
        }

        [TestMethod]
        public void CalculatePredictedDamage_WithNullArray_ReturnsMinusOne()
        {
            // Arrange
            int[]? hits = null;

            // Act
            var result = CreatureHelper.CalculatePredictedDamage(hits!);

            // Assert
            Assert.AreEqual(-1, result);
        }

        [DataTestMethod]
        [DataRow(0, 0)]
        [DataRow(1, 1)]
        [DataRow(29, 1)]
        [DataRow(30, 1)]
        [DataRow(31, 2)]
        [DataRow(59, 2)]
        [DataRow(60, 2)]
        [DataRow(61, 3)]
        [DataRow(600, 20)]
        [DataRow(601, 21)]
        public void CalculateTicksForClientTicks_WithVariousInputs_ReturnsCorrectTicks(int clientTicks, int expectedServerTicks)
        {
            // Act
            var result = CreatureHelper.CalculateTicksForClientTicks(clientTicks);

            // Assert
            Assert.AreEqual(expectedServerTicks, result);
        }
    }
}