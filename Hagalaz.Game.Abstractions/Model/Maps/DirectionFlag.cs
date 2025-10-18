using System;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Defines a set of bitwise flags representing the cardinal and intercardinal directions.
    /// </summary>
    [Flags]
    public enum DirectionFlag
    {
        /// <summary>
        /// Represents no specific direction.
        /// </summary>
        None = 99,
        /// <summary>
        /// The south direction.
        /// </summary>
        South = 0x1,
        /// <summary>
        /// The west direction.
        /// </summary>
        West = 0x2,
        /// <summary>
        /// The north direction.
        /// </summary>
        North = 0x4,
        /// <summary>
        /// The east direction.
        /// </summary>
        East = 0x8,
        /// <summary>
        /// The south-west direction.
        /// </summary>
        SouthWest = South | West,
        /// <summary>
        /// The north-west direction.
        /// </summary>
        NorthWest = North | West,
        /// <summary>
        /// The south-east direction.
        /// </summary>
        SouthEast = South | East,
        /// <summary>
        /// The north-east direction.
        /// </summary>
        NorthEast = North | East
    }
}