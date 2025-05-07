using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for working with game objects.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Filters the collection of game objects and returns only those that are standard objects.
        /// </summary>
        /// <param name="objects">The collection of game objects to filter.</param>
        /// <returns>
        /// A collection of game objects that are considered standard objects.
        /// </returns>
        public static IEnumerable<IGameObject> FindByStandardObject(this IEnumerable<IGameObject> objects) => objects.Where(obj => obj.IsStandard());

        /// <summary>
        /// Determines whether this instance is solid.
        /// </summary>
        /// <param name="object">The object.</param>
        /// <returns>
        ///   <c>true</c> if this instance is solid; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsStandard(this IGameObject @object) =>
            @object.ShapeType is ShapeType.ComplexGroundDecoration or ShapeType.GroundDefault or ShapeType.GroundDecoration;

        /// <summary>
        /// Determines whether [is roof top].
        /// </summary>
        /// <param name="object">The object.</param>
        /// <returns>
        ///   <c>true</c> if [is roof top]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRoofTop(this IGameObject @object) =>
            @object.ShapeType is ShapeType.RoofTopSide or ShapeType.RoofTopCornerFlat or ShapeType.RoofTopFlatDownwardCrease
                or ShapeType.RoofTopSlantedUpwardCrease or ShapeType.RoofTopSlantedDownwardCrease or ShapeType.RoofTopFlat;

        /// <summary>
        /// Determines whether this instance is wall.
        /// </summary>
        /// <param name="object">The object.</param>
        /// <returns>
        ///   <c>true</c> if this instance is wall; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsWall(this IGameObject @object) =>
            @object.ShapeType is ShapeType.Wall or ShapeType.WallCornerDiagonal or ShapeType.UnfinishedWall or ShapeType.WallCorner or ShapeType.WallOpen;

        /// <summary>
        /// Determines whether [is roof edge].
        /// </summary>
        /// <param name="object">The object.</param>
        /// <returns>
        ///   <c>true</c> if [is roof edge]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsRoofEdge(this IGameObject @object) =>
            @object.ShapeType is ShapeType.RoofEdge or ShapeType.RoofEdgeCornerFlat or ShapeType.RoofConnectingEdge or ShapeType.RoofEdgeCornerPointed;

        /// <summary>
        /// Determines whether [is wall decoration].
        /// </summary>
        /// <param name="object">The object.</param>
        /// <returns>
        ///   <c>true</c> if [is wall decoration] [the specified object]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsWallDecoration(this IGameObject @object) =>
            @object.ShapeType is ShapeType.WallDecorationStraightXOffset or ShapeType.WallDecorationStraightZOffset or ShapeType.WallDecorationDiagonalXOffset
                or ShapeType.WallDecorationDiagonalZOffset or ShapeType.InteriorWallDecorationDiagonal;

        /// <summary>
        /// Gets the region local hash including the layer.
        /// </summary>
        /// <param name="object"></param>
        /// <returns></returns>
        public static int GetRegionLocalHash(this IGameObject @object) =>
            GameObjectHelper.GetRegionLocalHash(@object.Location.X, @object.Location.Y, @object.Location.Z, (int)@object.ShapeType.GetLayerType());
    }
}