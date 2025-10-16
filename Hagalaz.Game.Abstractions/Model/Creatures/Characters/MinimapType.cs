namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines different states or configurations for the client's minimap display.
    /// </summary>
    public enum MinimapType : byte
    {
        /// <summary>
        /// The standard, visible minimap and compass.
        /// </summary>
        Standard = 0,
        /// <summary>
        /// The minimap is blacked out, but the compass remains visible.
        /// </summary>
        BlackMinimap = 2,
        /// <summary>
        /// The compass is blacked out, but the minimap remains visible.
        /// </summary>
        BlackCompass = 3,
        /// <summary>
        /// Both the minimap and the compass are blacked out.
        /// </summary>
        BlackMinimapBlackCompass = 5,
    }
}
