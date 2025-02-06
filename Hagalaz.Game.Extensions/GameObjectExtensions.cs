using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Determines whether this instance is solid.
        /// </summary>
        /// <param name="object">The object.</param>
        /// <returns>
        ///   <c>true</c> if this instance is solid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsGroundObject(this IGameObject @object) => @object.ShapeType == ShapeType.ComplexGroundDecoration ||
                @object.ShapeType == ShapeType.GroundDefault ||
                @object.ShapeType == ShapeType.GroundDecoration;

        /// <summary>
        /// Determines whether [is roof top].
        /// </summary>
        /// <param name="object">The object.</param>
        /// <returns>
        ///   <c>true</c> if [is roof top]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRoofTop(this IGameObject @object) => @object.ShapeType == ShapeType.RoofTopSide ||
                @object.ShapeType == ShapeType.RoofTopCornerFlat ||
                @object.ShapeType == ShapeType.RoofTopFlatDownwardCrease ||
                @object.ShapeType == ShapeType.RoofTopSlantedUpwardCrease ||
                @object.ShapeType == ShapeType.RoofTopSlantedDownwardCrease ||
                @object.ShapeType == ShapeType.RoofTopFlat;

        /// <summary>
        /// Determines whether this instance is wall.
        /// </summary>
        /// <param name="object">The object.</param>
        /// <returns>
        ///   <c>true</c> if this instance is wall; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsWall(this IGameObject @object) => @object.ShapeType == ShapeType.Wall ||
                @object.ShapeType == ShapeType.WallCornerDiagonal ||
                @object.ShapeType == ShapeType.UnfinishedWall ||
                @object.ShapeType == ShapeType.WallCorner ||
                @object.ShapeType == ShapeType.WallOpen;

        /// <summary>
        /// Determines whether [is roof edge].
        /// </summary>
        /// <param name="object">The object.</param>
        /// <returns>
        ///   <c>true</c> if [is roof edge]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRoofEdge(this IGameObject @object) => @object.ShapeType == ShapeType.RoofEdge ||
                @object.ShapeType == ShapeType.RoofEdgeCornerFlat ||
                @object.ShapeType == ShapeType.RoofConnectingEdge ||
                @object.ShapeType == ShapeType.RoofEdgeCornerPointed;

        /// <summary>
        /// Determines whether [is wall decoration].
        /// </summary>
        /// <param name="object">The object.</param>
        /// <returns>
        ///   <c>true</c> if [is wall decoration] [the specified object]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsWallDecoration(this IGameObject @object) => @object.ShapeType == ShapeType.WallDecorationStraightXOffset ||
                @object.ShapeType == ShapeType.WallDecorationStraightZOffset ||
                @object.ShapeType == ShapeType.WallDecorationDiagonalXOffset ||
                @object.ShapeType == ShapeType.WallDecorationDiagonalZOffset ||
                @object.ShapeType == ShapeType.InteriorWallDecorationDiagonal;

        /// <summary>
        /// Gets the region local hash including the layer.
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        public static int GetRegionLocalHash(this IGameObject @object) => GameObjectHelper.GetRegionLocalHash(@object.Location.X, @object.Location.Y, @object.Location.Z, (int)@object.ShapeType.GetLayerType());
    }
}
