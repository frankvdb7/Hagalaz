using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines a set of flags that indicate which parts of an item's appearance have been customized.
    /// </summary>
    [Flags]
    public enum ItemUpdateFlags
    {
        /// <summary>
        /// Indicates that the item's primary model has been changed.
        /// </summary>
        Model = 0x1,
        /// <summary>
        /// Indicates that the item's head model (when worn) has been changed.
        /// </summary>
        HeadModel = 0x2,
        /// <summary>
        /// Indicates that the item's colors have been changed.
        /// </summary>
        Color = 0x4,
        /// <summary>
        /// Indicates that the item's textures have been changed.
        /// </summary>
        Texture = 0x8,
    }
}
