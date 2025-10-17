using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines a set of flags used by the client to determine which parts of a character's appearance or state need to be updated.
    /// </summary>
    [Flags]
    public enum UpdateFlags
    {
        /// <summary>
        /// Indicates a temporary change in movement type.
        /// </summary>
        TemporaryMovementType = 0x8000,
        /// <summary>
        /// Indicates a permanent change in movement type (e.g., walking/running).
        /// </summary>
        MovementType = 0x80,
        /// <summary>
        /// Indicates a change in the character's appearance (equipment, colors, etc.).
        /// </summary>
        Appearance = 0x20,
        /// <summary>
        /// An expansion flag for byte-sized data.
        /// </summary>
        ExpandByte = 0x8,
        /// <summary>
        /// An expansion flag for short-sized data.
        /// </summary>
        ExpandShort = 0x2000,
        /// <summary>
        /// Indicates a new animation should be played.
        /// </summary>
        Animation = 0x40,
        /// <summary>
        /// Indicates a graphic should be displayed (slot 1).
        /// </summary>
        Graphic1 = 0x1,
        /// <summary>
        /// Indicates a graphic should be displayed (slot 2).
        /// </summary>
        Graphic2 = 0x100,
        /// <summary>
        /// Indicates a graphic should be displayed (slot 3).
        /// </summary>
        Graphic3 = 0x200000,
        /// <summary>
        /// Indicates a graphic should be displayed (slot 4).
        /// </summary>
        Graphic4 = 0x400000,
        /// <summary>
        /// Indicates new hitsplats should be displayed.
        /// </summary>
        Hits = 0x4,
        /// <summary>
        /// Indicates the character should face another creature.
        /// </summary>
        FaceCreature = 0x10,
        /// <summary>
        /// Indicates the character should turn to face a specific location.
        /// </summary>
        TurnTo = 0x2,
        /// <summary>
        /// Indicates the character is speaking.
        /// </summary>
        Speak = 0x400,
        /// <summary>
        /// Indicates a non-standard movement (e.g., knockback) should be rendered.
        /// </summary>
        NonStandardMovement = 0x200,
        /// <summary>
        /// Indicates the character's minimap dot should be orange.
        /// </summary>
        OrangeMiniMapDot = 0x100,
        /// <summary>
        /// Indicates the character's minimap dot should be purple.
        /// </summary>
        PWordMiniMapDot = 0x80000,
        /// <summary>
        /// Indicates a glow effect should be rendered on the character.
        /// </summary>
        Glow = 0x40000,
    }
}
