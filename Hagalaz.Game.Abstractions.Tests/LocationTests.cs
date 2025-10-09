// Copyright (c) Geta Digital. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Tests;

[TestClass]
public class LocationTests
{
    [TestMethod]
    public void Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        const int x = 10;
        const int y = 20;
        const int z = 5;
        const int dimension = 1;

        // Act
        var location = new Location(x, y, z, dimension);

        // Assert
        Assert.AreEqual(x, location.X);
        Assert.AreEqual(y, location.Y);
        Assert.AreEqual(z, location.Z);
        Assert.AreEqual(dimension, location.Dimension);
    }

    [TestMethod]
    public void Create_WithXY_ShouldSetCorrectDefaults()
    {
        // Arrange
        const int x = 15;
        const int y = 25;

        // Act
        var location = Location.Create(x, y);

        // Assert
        Assert.AreEqual(x, location.X);
        Assert.AreEqual(y, location.Y);
        Assert.AreEqual(0, location.Z);
        Assert.AreEqual(0, location.Dimension);
    }

    [TestMethod]
    public void Create_WithXYZ_ShouldSetCorrectDefaults()
    {
        // Arrange
        const int x = 15;
        const int y = 25;
        const int z = 3;

        // Act
        var location = Location.Create(x, y, z);

        // Assert
        Assert.AreEqual(x, location.X);
        Assert.AreEqual(y, location.Y);
        Assert.AreEqual(z, location.Z);
        Assert.AreEqual(0, location.Dimension);
    }

    [TestMethod]
    public void Create_WithXYZDimension_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        const int x = 10;
        const int y = 20;
        const int z = 5;
        const int dimension = 1;

        // Act
        var location = Location.Create(x, y, z, dimension);

        // Assert
        Assert.AreEqual(x, location.X);
        Assert.AreEqual(y, location.Y);
        Assert.AreEqual(z, location.Z);
        Assert.AreEqual(dimension, location.Dimension);
    }

    [DataTestMethod]
    [DataRow(3250, 3250, 406, 406, 50, 50, 12850)]
    [DataRow(3200, 3200, 400, 400, 0, 0, 12850)]
    public void Region_Properties_ShouldCalculateCorrectly(int x, int y, int expectedRegionPartX, int expectedRegionPartY, int expectedRegionLocalX, int expectedRegionLocalY, int expectedRegionId)
    {
        // Arrange
        var location = new Location(x, y, 0, 0);

        // Assert
        Assert.AreEqual(expectedRegionPartX, location.RegionPartX);
        Assert.AreEqual(expectedRegionPartY, location.RegionPartY);
        Assert.AreEqual(x >> 6, location.RegionX);
        Assert.AreEqual(y >> 6, location.RegionY);
        Assert.AreEqual(expectedRegionLocalX, location.RegionLocalX);
        Assert.AreEqual(expectedRegionLocalY, location.RegionLocalY);
        Assert.AreEqual(expectedRegionId, location.RegionId);
    }

    [TestMethod]
    public void Equals_ShouldReturnTrueForSameLocations()
    {
        // Arrange
        var location1 = new Location(10, 20, 5, 1);
        var location2 = new Location(10, 20, 5, 1);

        // Assert
        Assert.IsTrue(location1.Equals(location2));
        Assert.IsTrue(location1.Equals((object)location2));
        Assert.IsTrue(location1 == location2);
        Assert.IsFalse(location1 != location2);
    }

    [TestMethod]
    public void Equals_ShouldReturnFalseForDifferentLocations()
    {
        // Arrange
        var location1 = new Location(10, 20, 5, 1);
        var location2 = new Location(11, 20, 5, 1);
        var location3 = new Location(10, 21, 5, 1);
        var location4 = new Location(10, 20, 6, 1);
        var location5 = new Location(10, 20, 5, 2);

        // Assert
        Assert.IsFalse(location1.Equals(location2));
        Assert.IsFalse(location1.Equals(location3));
        Assert.IsFalse(location1.Equals(location4));
        Assert.IsFalse(location1.Equals(location5));
        Assert.IsFalse(location1.Equals(null));
        Assert.IsFalse(location1.Equals(new object()));
    }

    [TestMethod]
    public void GetHashCode_ShouldBeEqualForSameLocations()
    {
        // Arrange
        var location1 = new Location(10, 20, 1, 0);
        var location2 = new Location(10, 20, 1, 0);

        // Assert
        Assert.AreEqual(location1.GetHashCode(), location2.GetHashCode());
    }

    [TestMethod]
    public void GetHashCode_ShouldBeDifferentForDifferentLocations()
    {
        // Arrange
        var location1 = new Location(10, 20, 1, 0);
        var location2 = new Location(20, 10, 0, 1);

        // Assert
        Assert.AreNotEqual(location1.GetHashCode(), location2.GetHashCode());
    }

    [TestMethod]
    public void ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var location = new Location(10, 20, 5, 1);
        var expected = "x=10,y=20,z=5,dimension=1";

        // Act
        var result = location.ToString();

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Clone_ShouldCreateExactCopy()
    {
        // Arrange
        var original = new Location(10, 20, 5, 1);

        // Act
        var clone = original.Clone();

        // Assert
        Assert.AreEqual(original, clone);
        Assert.AreNotSame(original, clone);
    }

    [TestMethod]
    public void Copy_ShouldCreateCopyWithNewDimension()
    {
        // Arrange
        var original = new Location(10, 20, 5, 1);
        const int newDimension = 2;

        // Act
        var copy = original.Copy(newDimension);

        // Assert
        Assert.AreEqual(original.X, copy.X);
        Assert.AreEqual(original.Y, copy.Y);
        Assert.AreEqual(original.Z, copy.Z);
        Assert.AreEqual(newDimension, copy.Dimension);
    }

    [TestMethod]
    public void Translate_ShouldReturnTranslatedLocation()
    {
        // Arrange
        var location = new Location(10, 20, 5, 1);
        var expected = new Location(15, 25, 3, 1);

        // Act
        var result = location.Translate(5, 5, -2);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [DataTestMethod]
    [DataRow(10, 10, 10, 10, 0, true)]
    [DataRow(10, 10, 11, 10, 1, true)]
    [DataRow(10, 10, 10, 11, 1, true)]
    [DataRow(10, 10, 12, 10, 1, false)]
    [DataRow(10, 10, 10, 12, 1, false)]
    [DataRow(10, 10, 9, 10, 1, true)]
    [DataRow(10, 10, 10, 9, 1, true)]
    public void WithinDistance_ShouldReturnCorrectResult(int x1, int y1, int x2, int y2, int distance, bool expected)
    {
        // Arrange
        var location1 = new Location(x1, y1, 0, 0);
        var location2 = new Location(x2, y2, 0, 0);

        // Act
        var result = location1.WithinDistance(location2, distance);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetDistance_ShouldReturnCorrectDistance()
    {
        // Arrange
        var location1 = new Location(10, 10, 0, 0);
        var location2 = new Location(13, 14, 0, 0);
        const double expected = 5.0;

        // Act
        var result = location1.GetDistance(location2);

        // Assert
        Assert.AreEqual(expected, result, 0.001);
    }

    [TestMethod]
    public void Indexer_ShouldReturnCorrectValue()
    {
        // Arrange
        var location = new Location(10, 20, 5, 1);

        // Assert
        Assert.AreEqual(10, location[0]);
        Assert.AreEqual(20, location[1]);
        Assert.AreEqual(5, location[2]);
        Assert.AreEqual(1, location[3]);
        Assert.ThrowsException<IndexOutOfRangeException>(() => location[4]);
    }

    [TestMethod]
    public void Operator_Addition_ShouldAddLocations()
    {
        // Arrange
        var location1 = new Location(10, 20, 5, 1);
        var location2 = new Location(5, 10, 2, 1);
        var expected = new Location(15, 30, 7, 2);

        // Act
        var result = location1 + location2;

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Operator_Subtraction_ShouldSubtractLocations()
    {
        // Arrange
        var location1 = new Location(10, 20, 5, 1);
        var location2 = new Location(5, 10, 2, 0);
        var expected = new Location(5, 10, 3, 1);

        // Act
        var result = location1 - location2;

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Operator_Negation_ShouldNegateLocation()
    {
        // Arrange
        var location = new Location(10, -20, 5, 1);
        var expected = new Location(-10, 20, -5, 1);

        // Act
        var result = -location;

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void GetDirection_WithLocation_ShouldReturnCorrectDirection()
    {
        // Arrange
        var from = new Location(10, 10, 0, 0);
        var to = new Location(11, 11, 0, 0);
        var expected = DirectionFlag.NorthEast;

        // Act
        var result = from.GetDirection(to);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Static_GetDistance_ShouldReturnCorrectDistance()
    {
        // Arrange
        const double expected = 5.0;

        // Act
        var result = Location.GetDistance(10, 10, 13, 14);

        // Assert
        Assert.AreEqual(expected, result, 0.001);
    }

    [TestMethod]
    public void Static_GetDelta_ShouldReturnCorrectLocation()
    {
        // Arrange
        var from = new Location(10, 10, 2, 1);
        var to = new Location(15, 5, 3, 2);
        var expected = new Location(5, -5, 1, 1);

        // Act
        var result = Location.GetDelta(from, to);

        // Assert
        Assert.AreEqual(expected, result);
    }
}