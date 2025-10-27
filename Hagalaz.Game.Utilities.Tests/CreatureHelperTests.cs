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

        [DataTestMethod]
        [DynamicData(nameof(DamageTestData))]
        public void CalculatePredictedDamage_VariousInputs_CalculatesCorrectly(int[] hits, int expected)
        {
            Assert.AreEqual(expected, CreatureHelper.CalculatePredictedDamage(hits));
        }

        [TestMethod]
        public void CalculateTicksForClientTicks_ZeroClientTicks_ReturnsZero()
        {
            Assert.AreEqual(0, CreatureHelper.CalculateTicksForClientTicks(0));
        }

        [TestMethod]
        public void CalculateTicksForClientTicks_BoundaryValueJustBelowTick_ReturnsOne()
        {
            // (29 * 20 + 599) / 600 = 1179 / 600 = 1
            Assert.AreEqual(1, CreatureHelper.CalculateTicksForClientTicks(29));
        }

        [TestMethod]
        public void CalculateTicksForClientTicks_BoundaryValueAtTick_ReturnsOne()
        {
            // (30 * 20 + 599) / 600 = 1199 / 600 = 1
            Assert.AreEqual(1, CreatureHelper.CalculateTicksForClientTicks(30));
        }

        [TestMethod]
        public void CalculateTicksForClientTicks_BoundaryValueJustAboveTick_ReturnsTwo()
        {
            // (31 * 20 + 599) / 600 = 1219 / 600 = 2
            Assert.AreEqual(2, CreatureHelper.CalculateTicksForClientTicks(31));
        }

        [TestMethod]
        public void CalculateTicksForClientTicks_BoundaryValueAtTwoTicks_ReturnsTwo()
        {
            // (60 * 20 + 599) / 600 = 1799 / 600 = 2
            Assert.AreEqual(2, CreatureHelper.CalculateTicksForClientTicks(60));
        }

        [TestMethod]
        public void CalculateTicksForClientTicks_BoundaryValueJustAboveTwoTicks_ReturnsThree()
        {
            // (61 * 20 + 599) / 600 = 1819 / 600 = 3
            Assert.AreEqual(3, CreatureHelper.CalculateTicksForClientTicks(61));
        }
    }
}
