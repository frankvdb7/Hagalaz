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
        public static LayerType GetLayerType(this ShapeType shape) =>
            shape switch
            {
                ShapeType.Wall or ShapeType.WallCornerDiagonal or ShapeType.UnfinishedWall or ShapeType.WallCorner => LayerType.Walls,
                ShapeType.WallDecorationStraightXOffset or ShapeType.WallDecorationStraightZOffset or ShapeType.WallDecorationDiagonalXOffset
                    or ShapeType.WallDecorationDiagonalZOffset or ShapeType.InteriorWallDecorationDiagonal => LayerType.WallDecorations,
                ShapeType.WallOpen or ShapeType.ComplexGroundDecoration or ShapeType.GroundDefault or ShapeType.RoofTopSide or ShapeType.RoofTopCornerFlat
                    or ShapeType.RoofTopFlatDownwardCrease or ShapeType.RoofTopSlantedUpwardCrease or ShapeType.RoofTopSlantedDownwardCrease
                    or ShapeType.RoofTopFlat or ShapeType.RoofEdge or ShapeType.RoofEdgeCornerFlat or ShapeType.RoofConnectingEdge
                    or ShapeType.RoofEdgeCornerPointed => LayerType.StandardObjects,
                ShapeType.GroundDecoration => LayerType.FloorDecorations,
                _ => throw new NotImplementedException($"Layer for {nameof(ShapeType)} {shape} is not implemented")
            };
    }
}
