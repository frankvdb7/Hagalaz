using Hagalaz.Game.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Hagalaz.Game.Utilities.Tests
{
    [TestClass]
    public class CreatureHelperTests
    {
        public static IEnumerable<object[]> DamageTestData =>
            new List<object[]>
            {
                new object[] { null, -1 },
                new object[] { new int[0], -1 },
                new object[] { new[] { 0, 0, 0 }, 0 },
                new object[] { new[] { 10, -5, 5, 0 }, 15 },
                new object[] { new[] { 10, 15, 5 }, 30 },
                new object[] { new[] { -10, -5, 0 }, -1 },
                new object[] { new[] { int.MaxValue }, int.MaxValue },
                new object[] { new[] { 1000000000, 1000000000, 147483647 }, int.MaxValue },
                new object[] { new[] { int.MaxValue, -100, 50 }, int.MaxValue },
                new object[] { new[] { int.MinValue }, -1 },
                new object[] { new[] { -10, int.MinValue }, -1 },
            };

        [TestMethod]
        [DynamicData(nameof(DamageTestData))]
        public void CalculatePredictedDamage_VariousInputs_CalculatesCorrectly(int[] hits, int expected)
        {
            Assert.AreEqual(expected, CreatureHelper.CalculatePredictedDamage(hits));
        }

        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(29, 1)]
        [DataRow(30, 1)]
        [DataRow(31, 2)]
        [DataRow(60, 2)]
        [DataRow(61, 3)]
        public void CalculateTicksForClientTicks_VariousInputs_CalculatesCorrectly(int clientTicks, int expected)
        {
            Assert.AreEqual(expected, CreatureHelper.CalculateTicksForClientTicks(clientTicks));
        }

        [TestMethod]
        public void CalculatePredictedDamage_WithValuesThatOverflowInt_ReturnsIntMaxValue()
        {
            var hits = new[] { int.MaxValue - 50, 100, 200 }; // Sum > int.MaxValue
            var result = CreatureHelper.CalculatePredictedDamage(hits);
            Assert.AreEqual(int.MaxValue, result, "The method should cap the damage at int.MaxValue on overflow.");
        }
    }
}
