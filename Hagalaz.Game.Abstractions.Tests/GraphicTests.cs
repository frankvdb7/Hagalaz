// Copyright (c) Geta Digital. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Tests;

[TestClass]
public class GraphicTests
{
    [TestMethod]
    public void Graphic_Constructor_ShouldSetProperties()
    {
        // Arrange
        const int id = 1;
        const int delay = 2;
        const int height = 3;
        const int rotation = 4;

        // Act
        var graphic = new Graphic(id, delay, height, rotation);

        // Assert
        Assert.AreEqual(id, graphic.Id);
        Assert.AreEqual(delay, graphic.Delay);
        Assert.AreEqual(height, graphic.Height);
        Assert.AreEqual(rotation, graphic.Rotation);
    }

    [TestMethod]
    public void Graphic_Create_ShouldReturnGraphic()
    {
        // Arrange
        const int id = 1;
        const int delay = 2;
        const int height = 3;
        const int rotation = 4;

        // Act
        var graphic = (Graphic)Graphic.Create(id, delay, height, rotation);

        // Assert
        Assert.AreEqual(id, graphic.Id);
        Assert.AreEqual(delay, graphic.Delay);
        Assert.AreEqual(height, graphic.Height);
        Assert.AreEqual(rotation, graphic.Rotation);
    }

    [TestMethod]
    public void Graphic_Equals_ShouldReturnTrueForSameValues()
    {
        // Arrange
        var graphic1 = new Graphic(1, 2, 3, 4);
        var graphic2 = new Graphic(1, 2, 3, 4);

        // Assert
        Assert.IsTrue(graphic1.Equals(graphic2));
        Assert.IsTrue(graphic1 == graphic2);
        Assert.IsFalse(graphic1 != graphic2);
    }

    [TestMethod]
    public void Graphic_Equals_ShouldReturnFalseForDifferentValues()
    {
        // Arrange
        var graphic1 = new Graphic(1, 2, 3, 4);
        var graphic2 = new Graphic(5, 6, 7, 8);

        // Assert
        Assert.IsFalse(graphic1.Equals(graphic2));
        Assert.IsFalse(graphic1 == graphic2);
        Assert.IsTrue(graphic1 != graphic2);
    }

    [TestMethod]
    public void Graphic_GetHashCode_ShouldReturnSameValueForSameInstances()
    {
        // Arrange
        var graphic1 = new Graphic(1, 2, 3, 4);
        var graphic2 = new Graphic(1, 2, 3, 4);

        // Assert
        Assert.AreEqual(graphic1.GetHashCode(), graphic2.GetHashCode());
    }

    [TestMethod]
    public void Graphic_GetHashCode_ShouldReturnDifferentValueForDifferentInstances()
    {
        // Arrange
        var graphic1 = new Graphic(1, 2, 3, 4);
        var graphic2 = new Graphic(5, 6, 7, 8);

        // Assert
        Assert.AreNotEqual(graphic1.GetHashCode(), graphic2.GetHashCode());
    }
}