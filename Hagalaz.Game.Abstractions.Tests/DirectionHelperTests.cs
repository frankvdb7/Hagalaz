// Copyright (c) Geta Digital. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Tests;

[TestClass]
public class DirectionHelperTests
{
    [DataTestMethod]
    [DataRow(0, 1, 0)]   // North
    [DataRow(1, 1, 1)]   // NorthEast
    [DataRow(1, 0, 2)]   // East
    [DataRow(1, -1, 3)]  // SouthEast
    [DataRow(0, -1, 4)]  // South
    [DataRow(-1, -1, 5)] // SouthWest
    [DataRow(-1, 0, 6)]  // West
    [DataRow(-1, 1, 7)]  // NorthWest
    [DataRow(0, 0, -1)]  // No movement
    public void GetNpcMovementType_ShouldReturnCorrectType(int deltaX, int deltaY, int expected)
    {
        // Act
        var result = DirectionHelper.GetNpcMovementType(deltaX, deltaY);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [DataTestMethod]
    [DataRow(0, DirectionFlag.North)]
    [DataRow(1, DirectionFlag.NorthEast)]
    [DataRow(2, DirectionFlag.East)]
    [DataRow(3, DirectionFlag.SouthEast)]
    [DataRow(4, DirectionFlag.South)]
    [DataRow(5, DirectionFlag.SouthWest)]
    [DataRow(6, DirectionFlag.West)]
    [DataRow(7, DirectionFlag.NorthWest)]
    [DataRow(8, DirectionFlag.None)]
    [DataRow(-1, DirectionFlag.None)]
    public void GetNpcFaceDirection_ShouldReturnCorrectFlag(int faceDirection, DirectionFlag expected)
    {
        // Act
        var result = DirectionHelper.GetNpcFaceDirection(faceDirection);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [DataTestMethod]
    // Cardinal directions
    [DataRow(10, 10, 10, 11, DirectionFlag.North)]
    [DataRow(10, 10, 11, 10, DirectionFlag.East)]
    [DataRow(10, 10, 10, 9, DirectionFlag.South)]
    [DataRow(10, 10, 9, 10, DirectionFlag.West)]
    // Diagonal directions
    [DataRow(10, 10, 11, 11, DirectionFlag.NorthEast)]
    [DataRow(10, 10, 11, 9, DirectionFlag.SouthEast)]
    [DataRow(10, 10, 9, 9, DirectionFlag.SouthWest)]
    [DataRow(10, 10, 9, 11, DirectionFlag.NorthWest)]
    // No direction
    [DataRow(10, 10, 10, 10, DirectionFlag.None)]
    public void GetDirection_ShouldReturnCorrectFlag(int fromX, int fromY, int toX, int toY, DirectionFlag expected)
    {
        // Act
        var result = DirectionHelper.GetDirection(fromX, fromY, toX, toY);

        // Assert
        Assert.AreEqual(expected, result);
    }
}