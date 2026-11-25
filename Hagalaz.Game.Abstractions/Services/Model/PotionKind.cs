namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Identifies how a Herblore potion recipe consumes its ingredients. The values match the persisted kind of a potion definition.
    /// </summary>
    public enum PotionKind : byte
    {
        /// <summary>
        /// A primary ingredient turns a vial of water into the unfinished potion; one secondary ingredient turns the unfinished potion into the potion.
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Every secondary ingredient (the extreme potions) is consumed together with the base item (torstol) to make an overload.
        /// </summary>
        Overload = 1
    }
}
