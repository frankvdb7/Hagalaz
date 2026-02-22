using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Game.Abstractions.Tests.Model.Maps;

[TestClass]
public class DirectionHelperTests
{
    [TestMethod]
    public void DirectionDeltaX_ShouldHaveCorrectValues()
    {
        var expected = new sbyte[] { 0, 1, 0, -1, 1, 1, -1, -1 };
        CollectionAssert.AreEqual(expected, DirectionHelper.DirectionDeltaX);
    }

    [TestMethod]
    public void DirectionDeltaY_ShouldHaveCorrectValues()
    {
        var expected = new sbyte[] { 1, 0, -1, 0, 1, -1, 1, -1 };
        CollectionAssert.AreEqual(expected, DirectionHelper.DirectionDeltaY);
    }

    [TestMethod]
    public void ThreeBitsMovementType_ShouldHaveCorrectValues()
    {
        var expected = new[,]
        {
            { 0, 3, 5 },
            { 1, -1, 6 },
            { 2, 4, 7 }
        };
        CollectionAssert.AreEqual(expected, DirectionHelper.ThreeBitsMovementType);
    }

    [TestMethod]
    public void FourBitsMovementType_ShouldHaveCorrectValues()
    {
        var expected = new[,]
        {
            { 0, 5, 7, 9, 11 },
            { 1, -1, -1, -1, 12 },
            { 2, -1, -1, -1, 13 },
            { 3, -1, -1, -1, 14 },
            { 4, 6, 8, 10, 15 }
        };
        CollectionAssert.AreEqual(expected, DirectionHelper.FourBitsMovementType);
    }

    [TestMethod]
    public void RegionMovementType_ShouldHaveCorrectValues()
    {
        var expected = new[,]
        {
            { 0, 3, 5 },
            { 1, -1, 6 },
            { 2, 4, 7 }
        };
        CollectionAssert.AreEqual(expected, DirectionHelper.RegionMovementType);
    }

    [TestMethod]
    public void ThreeBitsNpcMovementType_ShouldHaveCorrectValues()
    {
        var expected = new[,]
        {
            { 5, 6, 7 },
            { 4, -1, 0 },
            { 3, 2, 1 }
        };
        CollectionAssert.AreEqual(expected, DirectionHelper.ThreeBitsNpcMovementType);
    }

    [TestMethod]
    [DataRow(0, 1, 0)]
    [DataRow(1, 1, 1)]
    [DataRow(1, 0, 2)]
    [DataRow(1, -1, 3)]
    [DataRow(0, -1, 4)]
    [DataRow(-1, -1, 5)]
    [DataRow(-1, 0, 6)]
    [DataRow(-1, 1, 7)]
    [DataRow(0, 0, -1)]
    public void GetNpcMovementType_ShouldReturnCorrectValue(int deltaX, int deltaY, int expected)
    {
        Assert.AreEqual(expected, DirectionHelper.GetNpcMovementType(deltaX, deltaY));
    }

    [TestMethod]
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
    public void GetNpcFaceDirection_ShouldReturnCorrectDirection(int faceDirection, DirectionFlag expected)
    {
        Assert.AreEqual(expected, DirectionHelper.GetNpcFaceDirection(faceDirection));
    }

    [TestMethod]
    [DataRow(0, 0, 0, 1, DirectionFlag.North)]
    [DataRow(0, 0, 0, -1, DirectionFlag.South)]
    [DataRow(0, 0, 1, 0, DirectionFlag.East)]
    [DataRow(0, 0, -1, 0, DirectionFlag.West)]
    [DataRow(0, 0, 1, 1, DirectionFlag.NorthEast)]
    [DataRow(0, 0, 1, -1, DirectionFlag.SouthEast)]
    [DataRow(0, 0, -1, 1, DirectionFlag.NorthWest)]
    [DataRow(0, 0, -1, -1, DirectionFlag.SouthWest)]
    [DataRow(0, 0, 0, 0, DirectionFlag.None)]
    public void GetDirection_ShouldReturnCorrectDirection(int fromX, int fromY, int toX, int toY, DirectionFlag expected)
    {
        Assert.AreEqual(expected, DirectionHelper.GetDirection(fromX, fromY, toX, toY));
    }
}