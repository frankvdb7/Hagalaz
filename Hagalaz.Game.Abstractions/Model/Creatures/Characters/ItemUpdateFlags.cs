using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Enum ItemUpdateFlags
    /// </summary>
    [Flags]
    public enum ItemUpdateFlags
    {
        Model = 0x1,
        HeadModel = 0x2,
        Color = 0x4,
        Texture = 0x8,
    }
}
