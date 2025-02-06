namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    /// <summary>
    /// 
    /// </summary>
    public enum ShapeType : byte
    {
        /// <summary>
        /// The wall
        /// </summary>
        Wall = 0,
        /// <summary>
        /// The wall corner diagonal
        /// </summary>
        WallCornerDiagonal = 1,
        /// <summary>
        /// The unfinished wall
        /// </summary>
        UnfinishedWall = 2,
        /// <summary>
        /// The wall corner
        /// </summary>
        WallCorner = 3,
        /// <summary>
        /// The wall decoration straight x offset
        /// </summary>
        WallDecorationStraightXOffset = 4,
        /// <summary>
        /// The wall decoration straight z offset
        /// </summary>
        WallDecorationStraightZOffset = 5,
        /// <summary>
        /// The wall decoration diagonal x offset
        /// </summary>
        WallDecorationDiagonalXOffset = 6,
        /// <summary>
        /// The wall decoration diagonal z offset
        /// </summary>
        WallDecorationDiagonalZOffset = 7,
        /// <summary>
        /// The interior wall decoration diagonal
        /// </summary>
        InteriorWallDecorationDiagonal = 8,
        /// <summary>
        /// The wall open
        /// </summary>
        WallOpen = 9,
        /// <summary>
        /// The complex ground decoration
        /// </summary>
        ComplexGroundDecoration = 10,
        /// <summary>
        /// The ground default
        /// </summary>
        GroundDefault = 11,
        /// <summary>
        /// The root top side
        /// </summary>
        RoofTopSide = 12,
        /// <summary>
        /// The roof top corner flat
        /// </summary>
        RoofTopCornerFlat = 13,
        /// <summary>
        /// The roof top flat downward crease
        /// </summary>
        RoofTopFlatDownwardCrease = 14,
        /// <summary>
        /// The roof top slanted upward crease
        /// </summary>
        RoofTopSlantedUpwardCrease = 15,
        /// <summary>
        /// The roof top slanted downward crease
        /// </summary>
        RoofTopSlantedDownwardCrease = 16,
        /// <summary>
        /// The roof top flat
        /// </summary>
        RoofTopFlat = 17,
        /// <summary>
        /// The roof edge
        /// </summary>
        RoofEdge = 18,
        /// <summary>
        /// The roof edge corner flat
        /// </summary>
        RoofEdgeCornerFlat = 19,
        /// <summary>
        /// The roof connecting edge
        /// </summary>
        RoofConnectingEdge = 20,
        /// <summary>
        /// The roof edge corner pointed
        /// </summary>
        RoofEdgeCornerPointed = 21,
        /// <summary>
        /// The ground decoration
        /// </summary>
        GroundDecoration = 22

    }
}
