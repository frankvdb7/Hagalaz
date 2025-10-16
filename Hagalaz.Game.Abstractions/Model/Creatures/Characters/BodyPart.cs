namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the different body parts of a character, used for appearance customization and equipment rendering.
    /// </summary>
    public enum BodyPart
    {
        /// <summary>
        /// The head slot, typically for hats and helmets.
        /// </summary>
        HatPart = 0,
        /// <summary>
        /// The back slot, for capes.
        /// </summary>
        CapePart = 1,
        /// <summary>
        /// The neck slot, for amulets and necklaces.
        /// </summary>
        AmuletPart = 2,
        /// <summary>
        /// The main-hand slot, for weapons.
        /// </summary>
        WeaponPart = 3,
        /// <summary>
        /// The torso slot, for shirts and body armor.
        /// </summary>
        ChestPart = 4,
        /// <summary>
        /// The off-hand slot, for shields and defenders.
        /// </summary>
        ShieldPart = 5,
        /// <summary>
        /// The arms slot, for sleeves or bracers.
        /// </summary>
        ArmsPart = 6,
        /// <summary>
        /// The legs slot, for trousers and leg armor.
        /// </summary>
        LegsPart = 7,
        /// <summary>
        /// The hair slot, for the character's hairstyle.
        /// </summary>
        HairPart = 8,
        /// <summary>
        /// The hands slot, for gloves and gauntlets.
        /// </summary>
        HandsPart = 9,
        /// <summary>
        /// The feet slot, for boots.
        /// </summary>
        FeetPart = 10,
        /// <summary>
        /// The facial hair slot, for beards and mustaches.
        /// </summary>
        BeardPart = 11,
        /// <summary>
        /// The aura slot, for cosmetic auras.
        /// </summary>
        AuraPart = 14,
    }
}
