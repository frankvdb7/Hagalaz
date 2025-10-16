namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the different types of dots that can represent a character on the minimap.
    /// </summary>
    public enum MiniMapDot
    {
        /// <summary>
        /// The standard white dot for a regular player.
        /// </summary>
        Standard,
        /// <summary>
        /// An orange dot, typically used for friends or clan members.
        /// </summary>
        OrangeDot,
        /// <summary>
        /// A purple dot, often used for specific quest-related characters or events.
        /// </summary>
        PWordDot,
    }
}