using System;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Extensions
{
    public static class ShapeTypeExtensions
    {
        /// <summary>
        /// Gets the layer for the shape
        /// </summary>
        /// <param name="shape"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static LayerType GetLayerType(this ShapeType shape)
        {
            if (shape == ShapeType.Wall || shape == ShapeType.WallCornerDiagonal || shape == ShapeType.UnfinishedWall || shape == ShapeType.WallCorner)
            {
                return LayerType.Walls;
            }
            else if (shape == ShapeType.WallDecorationStraightXOffset || shape == ShapeType.WallDecorationStraightZOffset || shape == ShapeType.WallDecorationDiagonalXOffset || shape == ShapeType.WallDecorationDiagonalZOffset ||
                shape == ShapeType.InteriorWallDecorationDiagonal)
            {
                return LayerType.WallDecorations;
            }
            else if (shape == ShapeType.WallOpen || shape == ShapeType.ComplexGroundDecoration || shape == ShapeType.GroundDefault ||
                shape == ShapeType.RoofTopSide || shape == ShapeType.RoofTopCornerFlat || shape == ShapeType.RoofTopFlatDownwardCrease || shape == ShapeType.RoofTopSlantedUpwardCrease || shape == ShapeType.RoofTopSlantedDownwardCrease ||
                shape == ShapeType.RoofTopFlat || shape == ShapeType.RoofEdge || shape == ShapeType.RoofEdgeCornerFlat || shape == ShapeType.RoofConnectingEdge || shape == ShapeType.RoofEdgeCornerPointed)
            {
                return LayerType.StandardObjects;
            }
            else if (shape == ShapeType.GroundDecoration)
            {
                return LayerType.FloorDecorations;
            }
            else
            {
                throw new NotImplementedException($"Layer for {nameof(ShapeType)} {shape} is not implemented");
            }
        }
    }
}
