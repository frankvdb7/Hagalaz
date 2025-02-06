using System;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum CollisionFlag
    {
        /// <summary>
        /// The walkable
        /// </summary>
        Walkable = 0x0,
        /// <summary>
        /// The wall north west
        /// </summary>
        WallNorthWest = 0x1,
        /// <summary>
        /// The wall north
        /// </summary>
        WallNorth = 0x2,
        /// <summary>
        /// The wall north east
        /// </summary>
        WallNorthEast = 0x4,
        /// <summary>
        /// The wall east
        /// </summary>
        WallEast = 0x8,
        /// <summary>
        /// The wall south east
        /// </summary>
        WallSouthEast = 0x10,
        /// <summary>
        /// The wall south
        /// </summary>
        WallSouth = 0x20,
        /// <summary>
        /// The wall south west
        /// </summary>
        WallSouthWest = 0x40,
        /// <summary>
        /// The wall west
        /// </summary>
        WallWest = 0x80,
        /// <summary>
        /// The object tile
        /// </summary>
        ObjectTile = 0x100,
        /// <summary>
        /// The blocked north west
        /// </summary>
        BlockedNorthWest = 0x200,
        /// <summary>
        /// The blocked north
        /// </summary>
        BlockedNorth = 0x400,
        /// <summary>
        /// The blocked north east
        /// </summary>
        BlockedNorthEast = 0x800,
        /// <summary>
        /// The blocked east
        /// </summary>
        BlockedEast = 0x1000,
        /// <summary>
        /// The blocked south east
        /// </summary>
        BlockedSouthEast = 0x2000,
        /// <summary>
        /// The blocked south
        /// </summary>
        BlockedSouth = 0x4000,
        /// <summary>
        /// The blocked south west
        /// </summary>
        BlockedSouthWest = 0x8000,
        /// <summary>
        /// The blocked west
        /// </summary>
        BlockedWest = 0x10000,
        /// <summary>
        /// The object block
        /// </summary>
        ObjectBlock = 0x20_000,
        /// <summary>
        /// The decoration block
        /// </summary>
        FloorDecorationBlock = 0x40_000,
        /// <summary>
        /// The unknown1
        /// </summary>
        Unknown1 = 0x80000,
        /// <summary>
        /// The unknown2
        /// </summary>
        Unknown2 = 0x100000,
        /// <summary>
        /// The water
        /// </summary>
        FloorBlock = 0x200_000,
        /// <summary>
        /// The wall allow range north west
        /// </summary>
        WallAllowRangeNorthWest = 0x400000,
        /// <summary>
        /// The wall allow range north
        /// </summary>
        WallAllowRangeNorth = 0x800000,
        /// <summary>
        /// The wall allow range north east
        /// </summary>
        WallAllowRangeNorthEast = 0x1000000,
        /// <summary>
        /// The wall allow range east
        /// </summary>
        WallAllowRangeEast = 0x2000000,
        /// <summary>
        /// The wall allow range south east
        /// </summary>
        WallAllowRangeSouthEast = 0x4000000,
        /// <summary>
        /// The wall allow range south
        /// </summary>
        WallAllowRangeSouth = 0x8000000,
        /// <summary>
        /// The wall allow range south west
        /// </summary>
        WallAllowRangeSouthWest = 0x10000000,
        /// <summary>
        /// The wall allow range west
        /// </summary>
        WallAllowRangeWest = 0x20000000,
        /// <summary>
        /// The object allow range
        /// </summary>
        ObjectAllowRange = 0x40000000,

        /// <summary>
        /// The check south
        /// </summary>
        CheckSouth = FloorBlock | FloorDecorationBlock | ObjectTile | WallSouth,
        /// <summary>
        /// The check west
        /// </summary>
        CheckWest = FloorBlock | FloorDecorationBlock | ObjectTile | WallWest,
        /// <summary>
        /// The check north
        /// </summary>
        CheckNorth = FloorBlock | FloorDecorationBlock | ObjectTile | WallNorth,
        /// <summary>
        /// The check east
        /// </summary>
        CheckEast = FloorBlock | FloorDecorationBlock | ObjectTile | WallEast,
        /// <summary>
        /// The check south west
        /// </summary>
        CheckSouthWest = CheckSouth | CheckWest | WallSouthWest,
        /// <summary>
        /// The check north west
        /// </summary>
        CheckNorthWest = CheckNorth | CheckWest | WallNorthWest,
        /// <summary>
        /// The check south east
        /// </summary>
        CheckSouthEast = CheckSouth | CheckEast | WallSouthEast,
        /// <summary>
        /// The check north east
        /// </summary>
        CheckNorthEast = CheckNorth | CheckEast | WallNorthEast,
        /// <summary>
        /// The check south variable
        /// </summary>
        CheckSouthVariable = CheckSouthWest | CheckSouthEast,
        /// <summary>
        /// The check west variable
        /// </summary>
        CheckWestVariable = CheckSouthWest | CheckNorthEast,
        /// <summary>
        /// The check north variable
        /// </summary>
        CheckNorthVariable = CheckNorthWest | CheckNorthEast,
        /// <summary>
        /// The check east variable
        /// </summary>
        CheckEastVariable = CheckSouthEast | CheckNorthEast,

        /// <summary>
        /// The traversable south
        /// 0x12c0102
        /// </summary>
        TraversableSouthBlocked = FloorDecorationBlock | FloorBlock | WallAllowRangeNorth | ObjectAllowRange,
        /// <summary>
        /// The traversable west
        /// 0x12c0108
        /// </summary>
        TraversableWestBlocked = FloorDecorationBlock | FloorBlock | WallAllowRangeEast | ObjectAllowRange,
        /// <summary>
        /// The traversable north
        /// 0x12c0120
        /// </summary>
        TraversableNorthBlocked = FloorDecorationBlock | FloorBlock | WallAllowRangeSouth | ObjectAllowRange,
        /// <summary>
        /// The traversable east
        /// 0x12c0180
        /// </summary>
        TraversableEastBlocked = FloorDecorationBlock | FloorBlock | WallAllowRangeWest | ObjectAllowRange,
        /// <summary>
        /// The traversable south west
        /// 0x12c010e
        /// </summary>
        TraversableSouthWestBlocked = TraversableSouthBlocked | TraversableWestBlocked | WallAllowRangeSouthWest,
        /// <summary>
        /// The traversable north west
        /// 0x12c0138
        /// </summary>
        TraversableNorthWestBlocked = TraversableNorthBlocked | TraversableWestBlocked | WallAllowRangeNorthWest,
        /// <summary>
        /// The traversable south east
        /// 0x12c0183
        /// </summary>
        TraversableSouthEastBlocked = TraversableSouthBlocked | TraversableEastBlocked | WallAllowRangeSouthEast,
        /// <summary>
        /// The traversable north east
        /// 0x12c01e0
        /// </summary>
        TraversableNorthEastBlocked = TraversableNorthBlocked | TraversableEastBlocked | WallAllowRangeNorthEast,
        /// <summary>
        /// The traversable variable south blocked
        /// 0x12c018f
        /// </summary>
        TraversableVariableSouthBlocked = TraversableSouthWestBlocked | TraversableSouthEastBlocked,
        /// <summary>
        /// The traversable variable west blocked
        /// 0x12c013e
        /// </summary>
        TraversableVariableWestBlocked = TraversableSouthWestBlocked | TraversableNorthWestBlocked,
        /// <summary>
        /// The traversable variable north blocked
        /// 0x12c01f8
        /// </summary>
        TraversableVariableNorthBlocked = TraversableNorthWestBlocked | TraversableNorthEastBlocked,
        /// <summary>
        /// The traversable variable east blocked
        /// 0x12c01e3
        /// </summary>
        TraversableVariableEastBlocked = TraversableSouthEastBlocked | TraversableNorthEastBlocked
    }
}
