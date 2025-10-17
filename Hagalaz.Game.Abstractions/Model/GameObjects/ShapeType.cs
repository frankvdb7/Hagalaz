namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    /// <summary>
    /// Defines the basic shape of a game object, which determines its rendering layer and clipping behavior.
    /// </summary>
    public enum ShapeType : byte
    {
        /// <summary>
        /// A straight wall piece.
        /// </summary>
        Wall = 0,
        /// <summary>
        /// A diagonal wall corner piece.
        /// </summary>
        WallCornerDiagonal = 1,
        /// <summary>
        /// An entire wall section, often used for large buildings.
        /// </summary>
        UnfinishedWall = 2,
        /// <summary>
        /// A standard wall corner piece.
        /// </summary>
        WallCorner = 3,
        /// <summary>
        /// A straight wall decoration with a horizontal offset.
        /// </summary>
        WallDecorationStraightXOffset = 4,
        /// <summary>
        /// A straight wall decoration with a vertical offset.
        /// </summary>
        WallDecorationStraightZOffset = 5,
        /// <summary>
        /// A diagonal wall decoration with a horizontal offset.
        /// </summary>
        WallDecorationDiagonalXOffset = 6,
        /// <summary>
        /// A diagonal wall decoration with a vertical offset.
        /// </summary>
        WallDecorationDiagonalZOffset = 7,
        /// <summary>
        /// A diagonal interior wall decoration.
        /// </summary>
        InteriorWallDecorationDiagonal = 8,
        /// <summary>
        /// A wall with a central opening, like a window or archway.
        /// </summary>
        WallOpen = 9,
        /// <summary>
        /// A complex ground decoration that may have multiple parts.
        /// </summary>
        ComplexGroundDecoration = 10,
        /// <summary>
        /// A standard ground-level object.
        /// </summary>
        GroundDefault = 11,
        /// <summary>
        /// A side piece of a rooftop.
        /// </summary>
        RoofTopSide = 12,
        /// <summary>
        /// A flat corner piece of a rooftop.
        /// </summary>
        RoofTopCornerFlat = 13,
        /// <summary>
        /// A flat rooftop piece with a downward crease.
        /// </summary>
        RoofTopFlatDownwardCrease = 14,
        /// <summary>
        /// A slanted rooftop piece with an upward crease.
        /// </summary>
        RoofTopSlantedUpwardCrease = 15,
        /// <summary>
        /// A slanted rooftop piece with a downward crease.
        /// </summary>
        RoofTopSlantedDownwardCrease = 16,
        /// <summary>
        /// A flat rooftop piece.
        /// </summary>
        RoofTopFlat = 17,
        /// <summary>
        /// An edge piece of a rooftop.
        /// </summary>
        RoofEdge = 18,
        /// <summary>
        /// A flat corner edge piece of a rooftop.
        /// </summary>
        RoofEdgeCornerFlat = 19,
        /// <summary>
        /// A connecting edge piece for a rooftop.
        /// </summary>
        RoofConnectingEdge = 20,
        /// <summary>
        /// A pointed corner edge piece for a rooftop.
        /// </summary>
        RoofEdgeCornerPointed = 21,
        /// <summary>
        /// A simple ground decoration.
        /// </summary>
        GroundDecoration = 22
    }
}