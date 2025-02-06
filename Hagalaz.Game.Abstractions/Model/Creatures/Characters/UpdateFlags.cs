using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Enum UpdateFlags
    /// </summary>
    [Flags]
    public enum UpdateFlags
    {
        /// <summary>
        /// 
        /// </summary>
        TemporaryMovementType = 0x8000,
        /// <summary>
        /// 
        /// </summary>
        MovementType = 0x80,
        /// <summary>
        /// 
        /// </summary>
        Appearance = 0x20,
        /// <summary>
        /// 
        /// </summary>
        ExpandByte = 0x8,
        /// <summary>
        /// 
        /// </summary>
        ExpandShort = 0x2000,
        /// <summary>
        /// 
        /// </summary>
        Animation = 0x40,
        /// <summary>
        /// 
        /// </summary>
        Graphic1 = 0x1,
        /// <summary>
        /// 
        /// </summary>
        Graphic2 = 0x100,
        /// <summary>
        /// 
        /// </summary>
        Graphic3 = 0x200000,
        /// <summary>
        /// 
        /// </summary>
        Graphic4 = 0x400000,
        /// <summary>
        /// 
        /// </summary>
        Hits = 0x4,
        /// <summary>
        /// 
        /// </summary>
        FaceCreature = 0x10,
        /// <summary>
        /// 
        /// </summary>
        TurnTo = 0x2,
        /// <summary>
        /// 
        /// </summary>
        Speak = 0x400,
        /// <summary>
        /// 
        /// </summary>
        NonStandardMovement = 0x200,
        /// <summary>
        /// 
        /// </summary>
        OrangeMiniMapDot = 0x100,
        /// <summary>
        /// 
        /// </summary>
        PWordMiniMapDot = 0x80000,
        /// <summary>
        /// 
        /// </summary>
        Glow = 0x40000,
    }
}
