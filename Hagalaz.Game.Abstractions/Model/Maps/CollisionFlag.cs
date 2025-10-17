using System;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Defines a set of bitwise flags that represent the collision properties of a map tile, used for pathfinding.
    /// </summary>
    [Flags]
    public enum CollisionFlag
    {
        /// <summary>
        /// A walkable tile with no collision flags.
        /// </summary>
        Walkable = 0x0,
        /// <summary>
        /// A wall blocking movement to the north-west.
        /// </summary>
        WallNorthWest = 0x1,
        /// <summary>
        /// A wall blocking movement to the north.
        /// </summary>
        WallNorth = 0x2,
        /// <summary>
        /// A wall blocking movement to the north-east.
        /// </summary>
        WallNorthEast = 0x4,
        /// <summary>
        /// A wall blocking movement to the east.
        /// </summary>
        WallEast = 0x8,
        /// <summary>
        /// A wall blocking movement to the south-east.
        /// </summary>
        WallSouthEast = 0x10,
        /// <summary>
        /// A wall blocking movement to the south.
        /// </summary>
        WallSouth = 0x20,
        /// <summary>
        /// A wall blocking movement to the south-west.
        /// </summary>
        WallSouthWest = 0x40,
        /// <summary>
        /// A wall blocking movement to the west.
        /// </summary>
        WallWest = 0x80,
        /// <summary>
        /// An object occupying the tile.
        /// </summary>
        ObjectTile = 0x100,
        /// <summary>
        /// A tile blocked by an object to the north-west.
        /// </summary>
        BlockedNorthWest = 0x200,
        /// <summary>
        /// A tile blocked by an object to the north.
        /// </summary>
        BlockedNorth = 0x400,
        /// <summary>
        /// A tile blocked by an object to the north-east.
        /// </summary>
        BlockedNorthEast = 0x800,
        /// <summary>
        /// A tile blocked by an object to the east.
        /// </summary>
        BlockedEast = 0x1000,
        /// <summary>
        /// A tile blocked by an object to the south-east.
        /// </summary>
        BlockedSouthEast = 0x2000,
        /// <summary>
        /// A tile blocked by an object to the south.
        /// </summary>
        BlockedSouth = 0x4000,
        /// <summary>
        /// A tile blocked by an object to the south-west.
        /// </summary>
        BlockedSouthWest = 0x8000,
        /// <summary>
        /// A tile blocked by an object to the west.
        /// </summary>
        BlockedWest = 0x10000,
        /// <summary>
        /// A tile blocked by an object, preventing movement.
        /// </summary>
        ObjectBlock = 0x20_000,
        /// <summary>
        /// A tile blocked by a floor decoration.
        /// </summary>
        FloorDecorationBlock = 0x40_000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Unknown1 = 0x80000,
        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Unknown2 = 0x100000,
        /// <summary>
        /// A tile blocked by water or another impassable floor type.
        /// </summary>
        FloorBlock = 0x200_000,
        /// <summary>
        /// A wall that allows ranged attacks over it to the north-west.
        /// </summary>
        WallAllowRangeNorthWest = 0x400000,
        /// <summary>
        /// A wall that allows ranged attacks over it to the north.
        /// </summary>
        WallAllowRangeNorth = 0x800000,
        /// <summary>
        /// A wall that allows ranged attacks over it to the north-east.
        /// </summary>
        WallAllowRangeNorthEast = 0x1000000,
        /// <summary>
        /// A wall that allows ranged attacks over it to the east.
        /// </summary>
        WallAllowRangeEast = 0x2000000,
        /// <summary>
        /// A wall that allows ranged attacks over it to the south-east.
        /// </summary>
        WallAllowRangeSouthEast = 0x4000000,
        /// <summary>
        /// A wall that allows ranged attacks over it to the south.
        /// </summary>
        WallAllowRangeSouth = 0x8000000,
        /// <summary>
        /// A wall that allows ranged attacks over it to the south-west.
        /// </summary>
        WallAllowRangeSouthWest = 0x10000000,
        /// <summary>
        /// A wall that allows ranged attacks over it to the west.
        /// </summary>
        WallAllowRangeWest = 0x20000000,
        /// <summary>
        /// An object that allows ranged attacks over it.
        /// </summary>
        ObjectAllowRange = 0x40000000,

        /// <summary>
        /// A combination of flags for checking movement to the south.
        /// </summary>
        CheckSouth = FloorBlock | FloorDecorationBlock | ObjectTile | WallSouth,
        /// <summary>
        /// A combination of flags for checking movement to the west.
        /// </summary>
        CheckWest = FloorBlock | FloorDecorationBlock | ObjectTile | WallWest,
        /// <summary>
        /// A combination of flags for checking movement to the north.
        /// </summary>
        CheckNorth = FloorBlock | FloorDecorationBlock | ObjectTile | WallNorth,
        /// <summary>
        /// A combination of flags for checking movement to the east.
        /// </summary>
        CheckEast = FloorBlock | FloorDecorationBlock | ObjectTile | WallEast,
        /// <summary>
        /// A combination of flags for checking movement to the south-west.
        /// </summary>
        CheckSouthWest = CheckSouth | CheckWest | WallSouthWest,
        /// <summary>
        /// A combination of flags for checking movement to the north-west.
        /// </summary>
        CheckNorthWest = CheckNorth | CheckWest | WallNorthWest,
        /// <summary>
        /// A combination of flags for checking movement to the south-east.
        /// </summary>
        CheckSouthEast = CheckSouth | CheckEast | WallSouthEast,
        /// <summary>
        /// A combination of flags for checking movement to the north-east.
        /// </summary>
        CheckNorthEast = CheckNorth | CheckEast | WallNorthEast,
        /// <summary>
        /// A combination of flags for checking diagonal movement to the south.
        /// </summary>
        CheckSouthVariable = CheckSouthWest | CheckSouthEast,
        /// <summary>
        /// A combination of flags for checking diagonal movement to the west.
        /// </summary>
        CheckWestVariable = CheckSouthWest | CheckNorthEast,
        /// <summary>
        /// A combination of flags for checking diagonal movement to the north.
        /// </summary>
        CheckNorthVariable = CheckNorthWest | CheckNorthEast,
        /// <summary>
        /// A combination of flags for checking diagonal movement to the east.
        /// </summary>
        CheckEastVariable = CheckSouthEast | CheckNorthEast,

        /// <summary>
        /// A combination of flags indicating a tile is blocked for traversal to the south.
        /// </summary>
        TraversableSouthBlocked = FloorDecorationBlock | FloorBlock | WallAllowRangeNorth | ObjectAllowRange,
        /// <summary>
        /// A combination of flags indicating a tile is blocked for traversal to the west.
        /// </summary>
        TraversableWestBlocked = FloorDecorationBlock | FloorBlock | WallAllowRangeEast | ObjectAllowRange,
        /// <summary>
        /// A combination of flags indicating a tile is blocked for traversal to the north.
        /// </summary>
        TraversableNorthBlocked = FloorDecorationBlock | FloorBlock | WallAllowRangeSouth | ObjectAllowRange,
        /// <summary>
        /// A combination of flags indicating a tile is blocked for traversal to the east.
        /// </summary>
        TraversableEastBlocked = FloorDecorationBlock | FloorBlock | WallAllowRangeWest | ObjectAllowRange,
        /// <summary>
        /// A combination of flags indicating a tile is blocked for traversal to the south-west.
        /// </summary>
        TraversableSouthWestBlocked = TraversableSouthBlocked | TraversableWestBlocked | WallAllowRangeSouthWest,
        /// <summary>
        /// A combination of flags indicating a tile is blocked for traversal to the north-west.
        /// </summary>
        TraversableNorthWestBlocked = TraversableNorthBlocked | TraversableWestBlocked | WallAllowRangeNorthWest,
        /// <summary>
        /// A combination of flags indicating a tile is blocked for traversal to the south-east.
        /// </summary>
        TraversableSouthEastBlocked = TraversableSouthBlocked | TraversableEastBlocked | WallAllowRangeSouthEast,
        /// <summary>
        /// A combination of flags indicating a tile is blocked for traversal to the north-east.
        /// </summary>
        TraversableNorthEastBlocked = TraversableNorthBlocked | TraversableEastBlocked | WallAllowRangeNorthEast,
        /// <summary>
        /// A combination of flags indicating a tile is blocked for diagonal traversal to the south.
        /// </summary>
        TraversableVariableSouthBlocked = TraversableSouthWestBlocked | TraversableSouthEastBlocked,
        /// <summary>
        /// A combination of flags indicating a tile is blocked for diagonal traversal to the west.
        /// </summary>
        TraversableVariableWestBlocked = TraversableSouthWestBlocked | TraversableNorthWestBlocked,
        /// <summary>
        /// A combination of flags indicating a tile is blocked for diagonal traversal to the north.
        /// </summary>
        TraversableVariableNorthBlocked = TraversableNorthWestBlocked | TraversableNorthEastBlocked,
        /// <summary>
        /// A combination of flags indicating a tile is blocked for diagonal traversal to the east.
        /// </summary>
        TraversableVariableEastBlocked = TraversableSouthEastBlocked | TraversableNorthEastBlocked
    }
}