using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
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
        ExpandByte = 0x4,
        /// <summary>
        /// 
        /// </summary>
        ExpandShort = 0x400,
        /// <summary>
        /// 
        /// </summary>
        ExpandInt = 0x20000,
        /// <summary>
        /// 
        /// </summary>
        FaceCreature = 0x20,
        /// <summary>
        /// 
        /// </summary>
        TurnTo = 0x2,
        /// <summary>
        /// 
        /// </summary>
        Transform = 0x80,
        /// <summary>
        /// 
        /// </summary>
        Speak = 0x10,
        /// <summary>
        /// 
        /// </summary>
        Animation = 0x1,
        /// <summary>
        /// 
        /// </summary>
        Graphic1 = 0x40,
        /// <summary>
        /// 
        /// </summary>
        Graphic2 = 0x200,
        /// <summary>
        /// 
        /// </summary>
        Graphic3 = 0x2000000,
        /// <summary>
        /// 
        /// </summary>
        Graphic4 = 0x1000000,
        /// <summary>
        /// 
        /// </summary>
        Hits = 0x8,
        /// <summary>
        /// 
        /// </summary>
        NonStandardMovement = 0x8000,
        /// <summary>
        /// 
        /// </summary>
        SetCombatLevel = 0x800000,
        /// <summary>
        /// 
        /// </summary>
        SetDisplayName = 0x40000,
        /// <summary>
        /// 
        /// </summary>
        Glow = 0x80000,
    }
}
