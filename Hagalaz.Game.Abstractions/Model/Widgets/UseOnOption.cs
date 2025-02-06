using System;

namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// The use on settings.
    /// 0x1 - ground object
    /// 0x2 - npc
    /// 0x4 - location
    /// 0x8 - player
    /// 0x10 - if1 inventory(type 2)
    /// 0x20 - if3 component
    /// 0x40 - tiles(minimap and 3dscreen)
    /// </summary>
    [Flags]
    public enum UseOnOption : byte
    {
        /// <summary>
        /// The ground items.
        /// </summary>
        GroundItems = 0x1,

        /// <summary>
        /// The npcs.
        /// </summary>
        Npcs = 0x2,

        /// <summary>
        /// The game objects
        /// </summary>
        GameObjects,

        /// <summary>
        /// The other characters.
        /// </summary>
        OtherCharacters = 0x8,

        /// <summary>
        /// The self character.
        /// </summary>
        SelfCharacter,

        /// <summary>
        /// The interface components.
        /// </summary>
        InterfaceComponents,
    }
}