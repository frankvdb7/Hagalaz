using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for working with <see cref="IGameObject"/> instances,
    /// simplifying checks for object types and other common operations.
    /// </summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Filters a collection of game objects, returning only those that are considered "standard" ground objects.
        /// </summary>
        /// <param name="objects">The collection of game objects to filter.</param>
        /// <returns>An enumerable collection containing only the standard game objects from the input.</returns>
        public static IEnumerable<IGameObject> FindByStandardObject(this IEnumerable<IGameObject> objects) => objects.Where(obj => obj.IsStandard());

        /// <summary>
        /// Checks if a game object is a standard ground object or decoration.
        /// </summary>
        /// <param name="object">The game object to check.</param>
        /// <returns><c>true</c> if the object's shape type is a standard ground or decoration type; otherwise, <c>false</c>.</returns>
        public static bool IsStandard(this IGameObject @object) =>
            @object.ShapeType is ShapeType.ComplexGroundDecoration or ShapeType.GroundDefault or ShapeType.GroundDecoration;

        /// <summary>
        /// Checks if a game object is part of a rooftop.
        /// </summary>
        /// <param name="object">The game object to check.</param>
        /// <returns><c>true</c> if the object's shape type corresponds to a rooftop component; otherwise, <c>false</c>.</returns>
        public static bool IsRoofTop(this IGameObject @object) =>
            @object.ShapeType is ShapeType.RoofTopSide or ShapeType.RoofTopCornerFlat or ShapeType.RoofTopFlatDownwardCrease
                or ShapeType.RoofTopSlantedUpwardCrease or ShapeType.RoofTopSlantedDownwardCrease or ShapeType.RoofTopFlat;

        /// <summary>
        /// Checks if a game object is a type of wall.
        /// </summary>
        /// <param name="object">The game object to check.</param>
        /// <returns><c>true</c> if the object's shape type corresponds to a wall component; otherwise, <c>false</c>.</returns>
        public static bool IsWall(this IGameObject @object) =>
            @object.ShapeType is ShapeType.Wall or ShapeType.WallCornerDiagonal or ShapeType.UnfinishedWall or ShapeType.WallCorner or ShapeType.WallOpen;

        /// <summary>
        /// Checks if a game object is part of a roof edge.
        /// </summary>
        /// <param name="object">The game object to check.</param>
        /// <returns><c>true</c> if the object's shape type corresponds to a roof edge component; otherwise, <c>false</c>.</returns>
        public static bool IsRoofEdge(this IGameObject @object) =>
            @object.ShapeType is ShapeType.RoofEdge or ShapeType.RoofEdgeCornerFlat or ShapeType.RoofConnectingEdge or ShapeType.RoofEdgeCornerPointed;

        /// <summary>
        /// Checks if a game object is a type of wall decoration.
        /// </summary>
        /// <param name="object">The game object to check.</param>
        /// <returns><c>true</c> if the object's shape type corresponds to a wall decoration; otherwise, <c>false</c>.</returns>
        public static bool IsWallDecoration(this IGameObject @object) =>
            @object.ShapeType is ShapeType.WallDecorationStraightXOffset or ShapeType.WallDecorationStraightZOffset or ShapeType.WallDecorationDiagonalXOffset
                or ShapeType.WallDecorationDiagonalZOffset or ShapeType.InteriorWallDecorationDiagonal;

        /// <summary>
        /// Calculates a unique hash for the game object's position within its region, taking into account its layer.
        /// </summary>
        /// <param name="object">The game object for which to calculate the hash.</param>
        /// <returns>An integer representing the object's unique hash within its region and layer.</returns>
        public static int GetRegionLocalHash(this IGameObject @object) =>
            GameObjectHelper.GetRegionLocalHash(@object.Location.X, @object.Location.Y, @object.Location.Z, (int)@object.ShapeType.GetLayerType());
    }
}