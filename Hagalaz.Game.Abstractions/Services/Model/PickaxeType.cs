namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Defines the different types of pickaxes, ordered by their effectiveness.
    /// </summary>
    public enum PickaxeType : byte
    {
        /// <summary>
        /// A bronze pickaxe.
        /// </summary>
        Bronze = 0,
        /// <summary>
        /// An iron pickaxe.
        /// </summary>
        Iron = 1,
        /// <summary>
        /// A steel pickaxe.
        /// </summary>
        Steel = 2,
        /// <summary>
        /// A mithril pickaxe.
        /// </summary>
        Mithril = 3,
        /// <summary>
        /// An adamant pickaxe.
        /// </summary>
        Adamant = 4,
        /// <summary>
        /// A rune pickaxe.
        /// </summary>
        Rune = 5,
        /// <summary>
        /// A dragon pickaxe.
        /// </summary>
        Dragon = 6
    }
}