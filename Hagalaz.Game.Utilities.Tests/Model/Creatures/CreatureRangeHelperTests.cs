using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Utilities.Model.Creatures;

namespace Hagalaz.Game.Utilities.Tests.Model.Creatures
{
    [TestClass]
    public class CreatureRangeHelperTests
    {
        [TestMethod]
        public void IsWithinRange_1x1_SameLocation_ReturnsTrue()
        {
            var loc = new Location(100, 100, 0, 0);
            Assert.IsTrue(CreatureRangeHelper.IsWithinRange(loc, 1, loc, 1, 0));
            Assert.IsTrue(CreatureRangeHelper.IsWithinRange(loc, 1, loc, 1, 1));
        }

        [TestMethod]
        public void IsWithinRange_1x1_Adjacent_ReturnsTrue()
        {
            var loc1 = new Location(100, 100, 0, 0);
            var loc2 = new Location(101, 100, 0, 0);

            Assert.IsTrue(CreatureRangeHelper.IsWithinRange(loc1, 1, loc2, 1, 1));
            Assert.IsFalse(CreatureRangeHelper.IsWithinRange(loc1, 1, loc2, 1, 0));
        }

        [TestMethod]
        public void IsWithinRange_1x1_Diagonal_ReturnsTrueAtRange1()
        {
            var loc1 = new Location(100, 100, 0, 0);
            var loc2 = new Location(101, 101, 0, 0);

            // sqrt(1^2 + 1^2) = 1.414. (int)1.414 = 1.
            Assert.IsTrue(CreatureRangeHelper.IsWithinRange(loc1, 1, loc2, 1, 1));
            Assert.IsFalse(CreatureRangeHelper.IsWithinRange(loc1, 1, loc2, 1, 0));
        }

        [TestMethod]
        public void IsWithinRange_3x3_Overlap_ReturnsTrue()
        {
            var loc1 = new Location(100, 100, 0, 0);
            var loc2 = new Location(102, 102, 0, 0); // Overlaps at (102,102)

            Assert.IsTrue(CreatureRangeHelper.IsWithinRange(loc1, 3, loc2, 3, 0));
        }

        [TestMethod]
        public void IsWithinRange_3x3_Boundary_ReturnsTrue()
        {
            var loc1 = new Location(100, 100, 0, 0); // (100,100) to (102,102)
            var loc2 = new Location(106, 100, 0, 0); // (106,100) to (108,102)

            // Distance from 102 to 106 is 4.
            Assert.IsTrue(CreatureRangeHelper.IsWithinRange(loc1, 3, loc2, 3, 4));
            Assert.IsFalse(CreatureRangeHelper.IsWithinRange(loc1, 3, loc2, 3, 3));
        }

        [TestMethod]
        public void IsWithinRange_DifferentPlane_ReturnsFalse()
        {
            var loc1 = new Location(100, 100, 0, 0);
            var loc2 = new Location(100, 100, 1, 0);

            Assert.IsFalse(CreatureRangeHelper.IsWithinRange(loc1, 1, loc2, 1, 10));
        }

        [TestMethod]
        public void IsWithinRange_DifferentDimension_ReturnsFalse()
        {
            var loc1 = new Location(100, 100, 0, 0);
            var loc2 = new Location(100, 100, 0, 1);

            Assert.IsFalse(CreatureRangeHelper.IsWithinRange(loc1, 1, loc2, 1, 10));
        }

        [TestMethod]
        public void IsWithinRange_NegativeRange_ReturnsFalse()
        {
            var loc = new Location(100, 100, 0, 0);
            // Even if overlapping, negative range should return false
            Assert.IsFalse(CreatureRangeHelper.IsWithinRange(loc, 1, loc, 1, -1));
            Assert.IsFalse(CreatureRangeHelper.IsWithinRange(loc, 1, loc, 1, -2));
        }

        [TestMethod]
        public void IsWithinRange_MaxRange_HandlesOverflow()
        {
            var loc1 = new Location(0, 0, 0, 0);
            var loc2 = new Location(10, 10, 0, 0);

            // Should not throw or fail due to overflow in range + 1
            Assert.IsTrue(CreatureRangeHelper.IsWithinRange(loc1, 1, loc2, 1, int.MaxValue));
        }
    }
}
