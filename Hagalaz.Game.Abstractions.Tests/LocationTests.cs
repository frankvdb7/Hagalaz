using Hagalaz.Game.Abstractions.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Game.Abstractions.Tests
{
    [TestClass]
    public class LocationTests
    {
        [TestMethod]
        public void Location_DefaultConstructor_ShouldCreateZeroLocation()
        {
            var location = new Location();
            Assert.AreEqual(0, location.X);
            Assert.AreEqual(0, location.Y);
            Assert.AreEqual(0, location.Z);
            Assert.AreEqual(0, location.Dimension);
        }

        [TestMethod]
        public void Location_Constructor_ShouldSetProperties()
        {
            var location = new Location(1, 2, 3, 4);
            Assert.AreEqual(1, location.X);
            Assert.AreEqual(2, location.Y);
            Assert.AreEqual(3, location.Z);
            Assert.AreEqual(4, location.Dimension);
        }

        [TestMethod]
        public void Create_WithXY_ShouldCreateCorrectLocation()
        {
            var location = Location.Create(1, 2);
            Assert.AreEqual(1, location.X);
            Assert.AreEqual(2, location.Y);
            Assert.AreEqual(0, location.Z);
            Assert.AreEqual(0, location.Dimension);
        }

        [TestMethod]
        public void Create_WithXYZ_ShouldCreateCorrectLocation()
        {
            var location = Location.Create(1, 2, 3);
            Assert.AreEqual(1, location.X);
            Assert.AreEqual(2, location.Y);
            Assert.AreEqual(3, location.Z);
            Assert.AreEqual(0, location.Dimension);
        }

        [TestMethod]
        public void Create_WithXYZDimension_ShouldCreateCorrectLocation()
        {
            var location = Location.Create(1, 2, 3, 4);
            Assert.AreEqual(1, location.X);
            Assert.AreEqual(2, location.Y);
            Assert.AreEqual(3, location.Z);
            Assert.AreEqual(4, location.Dimension);
        }

        [TestMethod]
        public void Clone_ShouldCreateIdenticalLocation()
        {
            var original = new Location(1, 2, 3, 4);
            var clone = original.Clone();
            Assert.AreEqual(original, clone);
        }

        [TestMethod]
        public void Copy_ShouldCreateIdenticalLocationWithNewDimension()
        {
            var original = new Location(1, 2, 3, 4);
            var copy = original.Copy(5);
            Assert.AreEqual(original.X, copy.X);
            Assert.AreEqual(original.Y, copy.Y);
            Assert.AreEqual(original.Z, copy.Z);
            Assert.AreEqual(5, copy.Dimension);
        }

        [TestMethod]
        public void Translate_ShouldReturnCorrectlyTranslatedLocation()
        {
            var original = new Location(1, 2, 3, 4);
            var translated = original.Translate(5, 6, 7);
            Assert.AreEqual(6, translated.X);
            Assert.AreEqual(8, translated.Y);
            Assert.AreEqual(10, translated.Z);
            Assert.AreEqual(4, translated.Dimension);
        }

        [TestMethod]
        public void GetHashCode_ShouldReturnConsistentValue()
        {
            var location1 = new Location(1, 2, 3, 4);
            var location2 = new Location(1, 2, 3, 4);
            Assert.AreEqual(location1.GetHashCode(), location2.GetHashCode());
        }

        [TestMethod]
        public void Equals_WithSameLocation_ShouldReturnTrue()
        {
            var location1 = new Location(1, 2, 3, 4);
            var location2 = new Location(1, 2, 3, 4);
            Assert.IsTrue(location1.Equals(location2));
        }

        [TestMethod]
        public void Equals_WithDifferentLocation_ShouldReturnFalse()
        {
            var location1 = new Location(1, 2, 3, 4);
            var location2 = new Location(5, 6, 7, 8);
            Assert.IsFalse(location1.Equals(location2));
        }

        [TestMethod]
        public void ToString_ShouldReturnCorrectFormat()
        {
            var location = new Location(1, 2, 3, 4);
            var expected = "x=1,y=2,z=3,dimension=4";
            Assert.AreEqual(expected, location.ToString());
        }

        [TestMethod]
        public void WithinDistance_WithSameLocation_ShouldReturnTrue()
        {
            var location1 = new Location(1, 2, 3, 4);
            var location2 = new Location(1, 2, 3, 4);
            Assert.IsTrue(location1.WithinDistance(location2, 0));
        }

        [TestMethod]
        public void WithinDistance_WithLocationWithinDistance_ShouldReturnTrue()
        {
            var location1 = new Location(1, 2, 3, 4);
            var location2 = new Location(2, 3, 3, 4);
            Assert.IsTrue(location1.WithinDistance(location2, 2));
        }

        [TestMethod]
        public void WithinDistance_WithLocationOutsideDistance_ShouldReturnFalse()
        {
            var location1 = new Location(1, 2, 3, 4);
            var location2 = new Location(5, 6, 3, 4);
            Assert.IsFalse(location1.WithinDistance(location2, 2));
        }

        [TestMethod]
        public void GetDistance_ShouldReturnCorrectDistance()
        {
            var location1 = new Location(0, 0, 0, 0);
            var location2 = new Location(3, 4, 0, 0);
            Assert.AreEqual(5, location1.GetDistance(location2));
        }

        [TestMethod]
        public void GetDelta_ShouldReturnCorrectDelta()
        {
            var from = new Location(1, 2, 3, 4);
            var to = new Location(5, 7, 9, 11);
            var delta = Location.GetDelta(from, to);
            Assert.AreEqual(4, delta.X);
            Assert.AreEqual(5, delta.Y);
            Assert.AreEqual(6, delta.Z);
            Assert.AreEqual(7, delta.Dimension);
        }

        [TestMethod]
        public void Operator_Equality_ShouldReturnTrueForEqualLocations()
        {
            var location1 = new Location(1, 2, 3, 4);
            var location2 = new Location(1, 2, 3, 4);
            Assert.IsTrue(location1 == location2);
        }

        [TestMethod]
        public void Operator_Inequality_ShouldReturnTrueForUnequalLocations()
        {
            var location1 = new Location(1, 2, 3, 4);
            var location2 = new Location(5, 6, 7, 8);
            Assert.IsTrue(location1 != location2);
        }

        [TestMethod]
        public void Operator_Addition_ShouldReturnCorrectSum()
        {
            var location1 = new Location(1, 2, 3, 4);
            var location2 = new Location(5, 6, 7, 8);
            var result = location1 + location2;
            Assert.AreEqual(6, result.X);
            Assert.AreEqual(8, result.Y);
            Assert.AreEqual(10, result.Z);
            Assert.AreEqual(12, result.Dimension);
        }

        [TestMethod]
        public void Operator_Subtraction_ShouldReturnCorrectDifference()
        {
            var location1 = new Location(5, 6, 7, 8);
            var location2 = new Location(1, 2, 3, 4);
            var result = location1 - location2;
            Assert.AreEqual(4, result.X);
            Assert.AreEqual(4, result.Y);
            Assert.AreEqual(4, result.Z);
            Assert.AreEqual(4, result.Dimension);
        }

        [TestMethod]
        public void Operator_Negation_ShouldReturnCorrectlyNegatedLocation()
        {
            var location = new Location(1, -2, 3, 4);
            var result = -location;
            Assert.AreEqual(-1, result.X);
            Assert.AreEqual(2, result.Y);
            Assert.AreEqual(-3, result.Z);
            Assert.AreEqual(4, result.Dimension);
        }
    }
}