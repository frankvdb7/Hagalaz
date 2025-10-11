// Copyright (c) Geta Digital. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Tests;

[TestClass]
public class AnimationTests
{
    [TestMethod]
    public void Animation_Constructor_ShouldSetProperties()
    {
        // Arrange
        const int id = 1;
        const int delay = 2;
        const int priority = 3;

        // Act
        var animation = new Animation(id, delay, priority);

        // Assert
        Assert.AreEqual(id, animation.Id);
        Assert.AreEqual(delay, animation.Delay);
        Assert.AreEqual(priority, animation.Priority);
    }

    [TestMethod]
    public void Animation_Create_ShouldReturnAnimation()
    {
        // Arrange
        const int id = 1;
        const int delay = 2;
        const int priority = 3;

        // Act
        var animation = Animation.Create(id, delay, priority);

        // Assert
        Assert.AreEqual(id, animation.Id);
        Assert.AreEqual(delay, animation.Delay);
        Assert.AreEqual(priority, animation.Priority);
    }

    [TestMethod]
    public void Animation_Create_WithResetValues_ShouldReturnResetAnimation()
    {
        // Arrange
        const int id = -1;
        const int delay = 0;
        const int priority = 0;

        // Act
        var animation = Animation.Create(id, delay, priority);

        // Assert
        Assert.AreEqual(Animation.Reset, animation);
    }

    [TestMethod]
    public void Animation_Equals_ShouldReturnTrueForSameValues()
    {
        // Arrange
        var animation1 = new Animation(1, 2, 3);
        var animation2 = new Animation(1, 2, 3);

        // Assert
        Assert.IsTrue(animation1.Equals(animation2));
        Assert.IsTrue(animation1 == animation2);
        Assert.IsFalse(animation1 != animation2);
    }

    [TestMethod]
    public void Animation_Equals_ShouldReturnFalseForDifferentValues()
    {
        // Arrange
        var animation1 = new Animation(1, 2, 3);
        var animation2 = new Animation(4, 5, 6);

        // Assert
        Assert.IsFalse(animation1.Equals(animation2));
        Assert.IsFalse(animation1 == animation2);
        Assert.IsTrue(animation1 != animation2);
    }

    [TestMethod]
    public void Animation_GetHashCode_ShouldReturnSameValueForSameInstances()
    {
        // Arrange
        var animation1 = new Animation(1, 2, 3);
        var animation2 = new Animation(1, 2, 3);

        // Assert
        Assert.AreEqual(animation1.GetHashCode(), animation2.GetHashCode());
    }

    [TestMethod]
    public void Animation_GetHashCode_ShouldReturnDifferentValueForDifferentInstances()
    {
        // Arrange
        var animation1 = new Animation(1, 2, 3);
        var animation2 = new Animation(4, 5, 6);

        // Assert
        Assert.AreNotEqual(animation1.GetHashCode(), animation2.GetHashCode());
    }
}