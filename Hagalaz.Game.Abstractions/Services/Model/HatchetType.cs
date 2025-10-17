namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Defines the different types of hatchets, ordered by their effectiveness.
    /// </summary>
    public enum HatchetType : byte
    {
        /// <summary>
        /// A bronze hatchet.
        /// </summary>
        Bronze = 0,
        /// <summary>
        /// An iron hatchet.
        /// </summary>
        Iron = 1,
        /// <summary>
        /// A steel hatchet.
        /// </summary>
        Steel = 2,
        /// <summary>
        /// A black hatchet.
        /// </summary>
        Black = 3,
        /// <summary>
        /// A mithril hatchet.
        /// </summary>
        Mithril = 4,
        /// <summary>
        /// An adamant hatchet.
        /// </summary>
        Adamant = 5,
        /// <summary>
        /// A rune hatchet.
        /// </summary>
        Rune = 6,
        /// <summary>
        /// A dragon hatchet.
        /// </summary>
        Dragon = 7
    }
}