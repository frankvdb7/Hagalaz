using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Extensions.Tests
{
    [TestClass]
    public class GameObjectExtensionsTests
    {
        [TestMethod]
        [DataRow(ShapeType.ComplexGroundDecoration, true)]
        [DataRow(ShapeType.GroundDefault, true)]
        [DataRow(ShapeType.GroundDecoration, true)]
        [DataRow(ShapeType.Wall, false)]
        [DataRow(ShapeType.RoofTopFlat, false)]
        public void IsStandard_ReturnsCorrectValue(ShapeType shapeType, bool expected)
        {
            // Arrange
            var gameObject = Substitute.For<IGameObject>();
            gameObject.ShapeType.Returns(shapeType);

            // Act
            var result = gameObject.IsStandard();

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        [DataRow(ShapeType.RoofTopSide, true)]
        [DataRow(ShapeType.RoofTopCornerFlat, true)]
        [DataRow(ShapeType.RoofTopFlatDownwardCrease, true)]
        [DataRow(ShapeType.RoofTopSlantedUpwardCrease, true)]
        [DataRow(ShapeType.RoofTopSlantedDownwardCrease, true)]
        [DataRow(ShapeType.RoofTopFlat, true)]
        [DataRow(ShapeType.Wall, false)]
        [DataRow(ShapeType.GroundDecoration, false)]
        public void IsRoofTop_ReturnsCorrectValue(ShapeType shapeType, bool expected)
        {
            // Arrange
            var gameObject = Substitute.For<IGameObject>();
            gameObject.ShapeType.Returns(shapeType);

            // Act
            var result = gameObject.IsRoofTop();

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        [DataRow(ShapeType.Wall, true)]
        [DataRow(ShapeType.WallCornerDiagonal, true)]
        [DataRow(ShapeType.UnfinishedWall, true)]
        [DataRow(ShapeType.WallCorner, true)]
        [DataRow(ShapeType.WallOpen, true)]
        [DataRow(ShapeType.RoofTopFlat, false)]
        [DataRow(ShapeType.GroundDecoration, false)]
        public void IsWall_ReturnsCorrectValue(ShapeType shapeType, bool expected)
        {
            // Arrange
            var gameObject = Substitute.For<IGameObject>();
            gameObject.ShapeType.Returns(shapeType);

            // Act
            var result = gameObject.IsWall();

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        [DataRow(ShapeType.RoofEdge, true)]
        [DataRow(ShapeType.RoofEdgeCornerFlat, true)]
        [DataRow(ShapeType.RoofConnectingEdge, true)]
        [DataRow(ShapeType.RoofEdgeCornerPointed, true)]
        [DataRow(ShapeType.Wall, false)]
        [DataRow(ShapeType.GroundDecoration, false)]
        public void IsRoofEdge_ReturnsCorrectValue(ShapeType shapeType, bool expected)
        {
            // Arrange
            var gameObject = Substitute.For<IGameObject>();
            gameObject.ShapeType.Returns(shapeType);

            // Act
            var result = gameObject.IsRoofEdge();

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        [DataRow(ShapeType.WallDecorationStraightXOffset, true)]
        [DataRow(ShapeType.WallDecorationStraightZOffset, true)]
        [DataRow(ShapeType.WallDecorationDiagonalXOffset, true)]
        [DataRow(ShapeType.WallDecorationDiagonalZOffset, true)]
        [DataRow(ShapeType.InteriorWallDecorationDiagonal, true)]
        [DataRow(ShapeType.Wall, false)]
        [DataRow(ShapeType.GroundDecoration, false)]
        public void IsWallDecoration_ReturnsCorrectValue(ShapeType shapeType, bool expected)
        {
            // Arrange
            var gameObject = Substitute.For<IGameObject>();
            gameObject.ShapeType.Returns(shapeType);

            // Act
            var result = gameObject.IsWallDecoration();

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void GetRegionLocalHash_ReturnsCorrectHash()
        {
            // Arrange
            var gameObject = Substitute.For<IGameObject>();
            gameObject.Location.Returns(new Location(10, 20, 1, 0));
            gameObject.ShapeType.Returns(ShapeType.GroundDecoration);

            // Act
            var result = gameObject.GetRegionLocalHash();

            // Assert
            Assert.AreEqual(103690, result);
        }
    }
}
