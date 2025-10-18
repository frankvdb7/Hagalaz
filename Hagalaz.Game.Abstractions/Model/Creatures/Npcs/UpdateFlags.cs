using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Defines a set of flags used by the client to determine which parts of an NPC's appearance or state need to be updated.
    /// </summary>
    [Flags]
    public enum UpdateFlags
    {
        /// <summary>
        /// An expansion flag for byte-sized data.
        /// </summary>
        ExpandByte = 0x4,
        /// <summary>
        /// An expansion flag for short-sized data.
        /// </summary>
        ExpandShort = 0x400,
        /// <summary>
        /// An expansion flag for int-sized data.
        /// </summary>
        ExpandInt = 0x20000,
        /// <summary>
        /// Indicates the NPC should face another creature.
        /// </summary>
        FaceCreature = 0x20,
        /// <summary>
        /// Indicates the NPC should turn to face a specific location.
        /// </summary>
        TurnTo = 0x2,
        /// <summary>
        /// Indicates the NPC's appearance has transformed into that of another NPC.
        /// </summary>
        Transform = 0x80,
        /// <summary>
        /// Indicates the NPC is speaking.
        /// </summary>
        Speak = 0x10,
        /// <summary>
        /// Indicates a new animation should be played.
        /// </summary>
        Animation = 0x1,
        /// <summary>
        /// Indicates a graphic should be displayed (slot 1).
        /// </summary>
        Graphic1 = 0x40,
        /// <summary>
        /// Indicates a graphic should be displayed (slot 2).
        /// </summary>
        Graphic2 = 0x200,
        /// <summary>
        /// Indicates a graphic should be displayed (slot 3).
        /// </summary>
        Graphic3 = 0x2000000,
        /// <summary>
        /// Indicates a graphic should be displayed (slot 4).
        /// </summary>
        Graphic4 = 0x1000000,
        /// <summary>
        /// Indicates new hitsplats should be displayed.
        /// </summary>
        Hits = 0x8,
        /// <summary>
        /// Indicates a non-standard movement (e.g., knockback) should be rendered.
        /// </summary>
        NonStandardMovement = 0x8000,
        /// <summary>
        /// Indicates the NPC's combat level has changed.
        /// </summary>
        SetCombatLevel = 0x800000,
        /// <summary>
        /// Indicates the NPC's display name has changed.
        /// </summary>
        SetDisplayName = 0x40000,
        /// <summary>
        /// Indicates a glow effect should be rendered on the NPC.
        /// </summary>
        Glow = 0x80000,
    }
}