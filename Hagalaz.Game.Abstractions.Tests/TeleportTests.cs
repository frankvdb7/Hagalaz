// Copyright (c) Geta Digital. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using NSubstitute;

namespace Hagalaz.Game.Abstractions.Tests;

[TestClass]
public class TeleportTests
{
    [TestMethod]
    public void Teleport_Create_WithLocationOnly_ShouldSetDefaultMovementType()
    {
        // Arrange
        var location = Substitute.For<ILocation>();

        // Act
        var teleport = Teleport.Create(location);

        // Assert
        Assert.AreEqual(location, teleport.Location);
        Assert.AreEqual(MovementType.Warp, teleport.Type);
    }

    [TestMethod]
    public void Teleport_Create_WithLocationAndMovementType_ShouldSetProperties()
    {
        // Arrange
        var location = Substitute.For<ILocation>();
        const MovementType type = MovementType.Walk;

        // Act
        var teleport = Teleport.Create(location, type);

        // Assert
        Assert.AreEqual(location, teleport.Location);
        Assert.AreEqual(type, teleport.Type);
    }
}