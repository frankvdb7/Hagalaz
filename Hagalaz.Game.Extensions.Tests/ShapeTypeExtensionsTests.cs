using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Extensions;
using System;

namespace Hagalaz.Game.Extensions.Tests
{
    [TestClass]
    public class ShapeTypeExtensionsTests
    {
        [DataTestMethod]
        [DataRow(ShapeType.Wall, LayerType.Walls)]
        [DataRow(ShapeType.WallCornerDiagonal, LayerType.Walls)]
        [DataRow(ShapeType.UnfinishedWall, LayerType.Walls)]
        [DataRow(ShapeType.WallCorner, LayerType.Walls)]
        [DataRow(ShapeType.WallDecorationStraightXOffset, LayerType.WallDecorations)]
        [DataRow(ShapeType.WallDecorationStraightZOffset, LayerType.WallDecorations)]
        [DataRow(ShapeType.WallDecorationDiagonalXOffset, LayerType.WallDecorations)]
        [DataRow(ShapeType.WallDecorationDiagonalZOffset, LayerType.WallDecorations)]
        [DataRow(ShapeType.InteriorWallDecorationDiagonal, LayerType.WallDecorations)]
        [DataRow(ShapeType.WallOpen, LayerType.StandardObjects)]
        [DataRow(ShapeType.ComplexGroundDecoration, LayerType.StandardObjects)]
        [DataRow(ShapeType.GroundDefault, LayerType.StandardObjects)]
        [DataRow(ShapeType.RoofTopSide, LayerType.StandardObjects)]
        [DataRow(ShapeType.RoofTopFlatDownwardCrease, LayerType.StandardObjects)]
        [DataRow(ShapeType.RoofTopSlantedUpwardCrease, LayerType.StandardObjects)]
        [DataRow(ShapeType.RoofTopSlantedDownwardCrease, LayerType.StandardObjects)]
        [DataRow(ShapeType.RoofTopFlat, LayerType.StandardObjects)]
        [DataRow(ShapeType.RoofEdge, LayerType.StandardObjects)]
        [DataRow(ShapeType.RoofEdgeCornerFlat, LayerType.StandardObjects)]
        [DataRow(ShapeType.RoofConnectingEdge, LayerType.StandardObjects)]
        [DataRow(ShapeType.RoofEdgeCornerPointed, LayerType.StandardObjects)]
        [DataRow(ShapeType.GroundDecoration, LayerType.FloorDecorations)]
        public void GetLayerType_ReturnsCorrectLayerType(ShapeType shape, LayerType expectedLayer)
        {
            var result = shape.GetLayerType();
            Assert.AreEqual(expectedLayer, result, $"The layer for {shape} was not {expectedLayer}.");
        }

        [TestMethod]
        public void GetLayerType_WithInvalidShape_ThrowsNotImplementedException()
        {
            Assert.ThrowsException<NotImplementedException>(() => ((ShapeType)255).GetLayerType());
        }
    }
}
