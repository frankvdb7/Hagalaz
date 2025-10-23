using System;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides an extension method for the <see cref="ShapeType"/> enum.
    /// </summary>
    public static class ShapeTypeExtensions
    {
        /// <summary>
        /// Gets the rendering and interaction layer associated with a given game object shape type.
        /// </summary>
        /// <param name="shape">The shape type to get the layer for.</param>
        /// <returns>The <see cref="LayerType"/> that corresponds to the specified shape.</returns>
        /// <exception cref="NotImplementedException">Thrown when a mapping for the specified <paramref name="shape"/> has not been defined.</exception>
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
