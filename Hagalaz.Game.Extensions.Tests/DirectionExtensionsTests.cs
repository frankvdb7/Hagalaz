using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Game.Extensions.Tests
{
    [TestClass]
    public class DirectionExtensionsTests
    {
        [DataTestMethod]
        [DataRow(DirectionFlag.North, DirectionFlag.South)]
        [DataRow(DirectionFlag.East, DirectionFlag.West)]
        [DataRow(DirectionFlag.South, DirectionFlag.North)]
        [DataRow(DirectionFlag.West, DirectionFlag.East)]
        [DataRow(DirectionFlag.NorthEast, DirectionFlag.SouthWest)]
        [DataRow(DirectionFlag.SouthEast, DirectionFlag.NorthWest)]
        [DataRow(DirectionFlag.SouthWest, DirectionFlag.NorthEast)]
        [DataRow(DirectionFlag.NorthWest, DirectionFlag.SouthEast)]
        [DataRow(DirectionFlag.None, DirectionFlag.None)]
        public void Reverse_ReturnsOppositeDirection(DirectionFlag original, DirectionFlag expected)
        {
            var result = original.Reverse();
            Assert.AreEqual(expected, result, $"The reverse of {original} was not {expected}.");
        }

        [DataTestMethod]
        [DataRow(DirectionFlag.North, 0)]
        [DataRow(DirectionFlag.East, 1)]
        [DataRow(DirectionFlag.South, 0)]
        [DataRow(DirectionFlag.West, -1)]
        [DataRow(DirectionFlag.NorthEast, 1)]
        [DataRow(DirectionFlag.SouthEast, 1)]
        [DataRow(DirectionFlag.SouthWest, -1)]
        [DataRow(DirectionFlag.NorthWest, -1)]
        [DataRow(DirectionFlag.None, 0)]
        public void GetDeltaX_ReturnsCorrectValue(DirectionFlag direction, int expectedDeltaX)
        {
            var result = direction.GetDeltaX();
            Assert.AreEqual(expectedDeltaX, result, $"The X delta for {direction} was incorrect.");
        }

        [DataTestMethod]
        [DataRow(DirectionFlag.North, 1)]
        [DataRow(DirectionFlag.East, 0)]
        [DataRow(DirectionFlag.South, -1)]
        [DataRow(DirectionFlag.West, 0)]
        [DataRow(DirectionFlag.NorthEast, 1)]
        [DataRow(DirectionFlag.SouthEast, -1)]
        [DataRow(DirectionFlag.SouthWest, -1)]
        [DataRow(DirectionFlag.NorthWest, 1)]
        [DataRow(DirectionFlag.None, 0)]
        public void GetDeltaY_ReturnsCorrectValue(DirectionFlag direction, int expectedDeltaY)
        {
            var result = direction.GetDeltaY();
            Assert.AreEqual(expectedDeltaY, result, $"The Y delta for {direction} was incorrect.");
        }

        [DataTestMethod]
        [DataRow(DirectionFlag.North, FaceDirection.North)]
        [DataRow(DirectionFlag.East, FaceDirection.East)]
        [DataRow(DirectionFlag.South, FaceDirection.South)]
        [DataRow(DirectionFlag.West, FaceDirection.West)]
        [DataRow(DirectionFlag.NorthEast, FaceDirection.NorthEast)]
        [DataRow(DirectionFlag.SouthEast, FaceDirection.SouthEast)]
        [DataRow(DirectionFlag.SouthWest, FaceDirection.SouthWest)]
        [DataRow(DirectionFlag.NorthWest, FaceDirection.NorthWest)]
        [DataRow(DirectionFlag.None, FaceDirection.None)]
        public void ToFaceDirection_ReturnsCorrectFaceDirection(DirectionFlag direction, FaceDirection expected)
        {
            var result = direction.ToFaceDirection();
            Assert.AreEqual(expected, result, $"The FaceDirection for {direction} was not {expected}.");
        }
    }
}