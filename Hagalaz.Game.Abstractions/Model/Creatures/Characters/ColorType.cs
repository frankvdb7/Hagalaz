namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the different parts of a character's appearance that can be customized with a color.
    /// </summary>
    public enum ColorType : byte
    {
        /// <summary>
        /// The character's hair color.
        /// </summary>
        HairColor = 0,
        /// <summary>
        /// The character's torso or shirt color.
        /// </summary>
        TorsoColor = 1,
        /// <summary>
        /// The character's leg or pants color.
        /// </summary>
        LegColor = 2,
        /// <summary>
        /// The character's feet or shoe color.
        /// </summary>
        FeetColor = 3,
        /// <summary>
        /// The character's skin color.
        /// </summary>
        SkinColor = 4,
    }
}
