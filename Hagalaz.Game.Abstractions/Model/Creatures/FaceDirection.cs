namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the cardinal and intercardinal directions that a creature can face.
    /// </summary>
    public enum FaceDirection
    {
        /// <summary>
        /// Facing East.
        /// </summary>
        East = 1,
        /// <summary>
        /// Facing North.
        /// </summary>
        North = 0,
        /// <summary>
        /// Facing North-East.
        /// </summary>
        NorthEast = 4,
        /// <summary>
        /// Facing North-West.
        /// </summary>
        NorthWest = 6,
        /// <summary>
        /// Facing South.
        /// </summary>
        South = 2,
        /// <summary>
        /// Facing South-East.
        /// </summary>
        SouthEast = 5,
        /// <summary>
        /// Facing South-West.
        /// </summary>
        SouthWest = 7,
        /// <summary>
        /// Facing West.
        /// </summary>
        West = 3,
        /// <summary>
        /// No specific direction.
        /// </summary>
        None = -1
    }
}
