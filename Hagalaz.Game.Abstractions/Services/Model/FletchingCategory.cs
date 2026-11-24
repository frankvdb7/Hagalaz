namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Identifies the kind of a Fletching recipe. The values match the persisted category of a fletching definition; 0 means no category.
    /// </summary>
    public enum FletchingCategory : byte
    {
        /// <summary>
        /// Cutting logs into unstrung bows and crossbow limbs.
        /// </summary>
        Wood = 1,

        /// <summary>
        /// Stringing bows and assembling crossbows.
        /// </summary>
        Bow = 2,

        /// <summary>
        /// Arrows, bolts, darts and other ammunition.
        /// </summary>
        Ammo = 3,

        /// <summary>
        /// Cutting gems into bolt tips.
        /// </summary>
        Tips = 4
    }
}
