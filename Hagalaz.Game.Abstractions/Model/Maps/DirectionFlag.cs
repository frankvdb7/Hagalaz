using System;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum DirectionFlag
    {
        /// <summary>
        /// The none
        /// </summary>
        None = 99,
        /// <summary>
        /// The south
        /// </summary>
        South = 0x1,
        /// <summary>
        /// The west
        /// </summary>
        West = 0x2,
        /// <summary>
        /// The north
        /// </summary>
        North = 0x4,
        /// <summary>
        /// The east
        /// </summary>
        East = 0x8,
        /// <summary>
        /// The south west
        /// </summary>
        SouthWest = South | West,
        /// <summary>
        /// The north west
        /// </summary>
        NorthWest = North | West,
        /// <summary>
        /// The south east
        /// </summary>
        SouthEast = South | East,
        /// <summary>
        /// The north east
        /// </summary>
        NorthEast = North | East
    }
}
