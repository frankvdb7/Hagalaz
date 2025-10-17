using System;

namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// Defines a set of bitwise flags that specify what types of entities an item or component can be used on.
    /// </summary>
    [Flags]
    public enum UseOnOption : byte
    {
        /// <summary>
        /// Can be used on ground items.
        /// </summary>
        GroundItems = 0x1,
        /// <summary>
        /// Can be used on NPCs.
        /// </summary>
        Npcs = 0x2,
        /// <summary>
        /// Can be used on game objects.
        /// </summary>
        GameObjects = 0x4,
        /// <summary>
        /// Can be used on other players.
        /// </summary>
        OtherCharacters = 0x8,
        /// <summary>
        /// Can be used on the player themselves.
        /// </summary>
        SelfCharacter = 0x10,
        /// <summary>
        /// Can be used on other interface components.
        /// </summary>
        InterfaceComponents = 0x20,
    }
}