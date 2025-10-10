using System;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Game.Extensions.Tests
{
    [TestClass]
    public class ShapeTypeExtensionsTests
    {
        [DataTestMethod]
        [DataRow(ShapeType.Wall)]
        [DataRow(ShapeType.WallCornerDiagonal)]
        [DataRow(ShapeType.UnfinishedWall)]
        [DataRow(ShapeType.WallCorner)]
        public void GetLayerType_ShouldReturnWalls_ForWallShapes(ShapeType shape)
        {
            var layerType = shape.GetLayerType();
            Assert.AreEqual(LayerType.Walls, layerType);
        }

        [DataTestMethod]
        [DataRow(ShapeType.WallDecorationStraightXOffset)]
        [DataRow(ShapeType.WallDecorationStraightZOffset)]
        [DataRow(ShapeType.WallDecorationDiagonalXOffset)]
        [DataRow(ShapeType.WallDecorationDiagonalZOffset)]
        [DataRow(ShapeType.InteriorWallDecorationDiagonal)]
        public void GetLayerType_ShouldReturnWallDecorations_ForWallDecorationShapes(ShapeType shape)
        {
            var layerType = shape.GetLayerType();
            Assert.AreEqual(LayerType.WallDecorations, layerType);
        }

        [DataTestMethod]
        [DataRow(ShapeType.WallOpen)]
        [DataRow(ShapeType.ComplexGroundDecoration)]
        [DataRow(ShapeType.GroundDefault)]
        [DataRow(ShapeType.RoofTopSide)]
        [DataRow(ShapeType.RoofTopCornerFlat)]
        [DataRow(ShapeType.RoofTopFlatDownwardCrease)]
        [DataRow(ShapeType.RoofTopSlantedUpwardCrease)]
        [DataRow(ShapeType.RoofTopSlantedDownwardCrease)]
        [DataRow(ShapeType.RoofTopFlat)]
        [DataRow(ShapeType.RoofEdge)]
        [DataRow(ShapeType.RoofEdgeCornerFlat)]
        [DataRow(ShapeType.RoofConnectingEdge)]
        [DataRow(ShapeType.RoofEdgeCornerPointed)]
        public void GetLayerType_ShouldReturnStandardObjects_ForStandardObjectShapes(ShapeType shape)
        {
            var layerType = shape.GetLayerType();
            Assert.AreEqual(LayerType.StandardObjects, layerType);
        }

        [TestMethod]
        public void GetLayerType_ShouldReturnFloorDecorations_ForGroundDecorationShape()
        {
            var shape = ShapeType.GroundDecoration;
            var layerType = shape.GetLayerType();
            Assert.AreEqual(LayerType.FloorDecorations, layerType);
        }

        [TestMethod]
        public void GetLayerType_ShouldThrowNotImplementedException_ForUnhandledShape()
        {
            var unhandledShape = unchecked((ShapeType)int.MaxValue);
            Assert.ThrowsException<NotImplementedException>(() => unhandledShape.GetLayerType());
        }
    }
}