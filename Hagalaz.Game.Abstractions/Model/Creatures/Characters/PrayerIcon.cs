namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// <summary>
    /// Defines the different icons that can be displayed above a character's head to indicate an active prayer or curse.
    /// </summary>
    public enum PrayerIcon : byte
    {
        /// <summary>
        /// No icon is displayed.
        /// </summary>
        None = unchecked((byte)-1),
        /// <summary>
        /// The icon for the Protect from Melee prayer.
        /// </summary>
        ProtectFromMelee = 0,
        /// <summary>
        /// The icon for the Protect from Missiles prayer.
        /// </summary>
        ProtectFromRange = 1,
        /// <summary>
        /// The icon for the Protect from Magic prayer.
        /// </summary>
        ProtectFromMagic = 2,
        /// <summary>
        /// The icon for the Retribution prayer.
        /// </summary>
        Retribution = 3,
        /// <summary>
        /// The icon for the Smite prayer.
        /// </summary>
        Smite = 4,
        /// <summary>
        /// The icon for the Redemption prayer.
        /// </summary>
        Redemption = 5,
        /// <summary>
        /// An icon representing a magic arrow.
        /// </summary>
        MageArrow = 6,
        /// <summary>
        /// The icon for the Protect from Summoning prayer.
        /// </summary>
        Summoning = 7,
        /// <summary>
        /// A combined icon for Melee and Summoning protection.
        /// </summary>
        MeleeSummoning = 8,
        /// <summary>
        /// A combined icon for Ranged and Summoning protection.
        /// </summary>
        RangeSummoning = 9,
        /// <summary>
        /// A combined icon for Magic and Summoning protection.
        /// </summary>
        MageSummoning = 10,
        /// <summary>
        /// An empty or placeholder icon.
        /// </summary>
        Empty = 11,
        /// <summary>
        /// The icon for the Deflect Melee curse.
        /// </summary>
        DeflectMelee = 12,
        /// <summary>
        /// The icon for the Deflect Magic curse.
        /// </summary>
        DeflectMage = 13,
        /// <summary>
        /// The icon for the Deflect Missiles curse.
        /// </summary>
        DeflectRange = 14,
        /// <summary>
        /// The icon for the Deflect Summoning curse.
        /// </summary>
        DeflectSummoning = 15,
        /// <summary>
        /// A combined icon for Deflect Melee and Deflect Summoning.
        /// </summary>
        DeflectMeleeSummoning = 16,
        /// <summary>
        /// A combined icon for Deflect Ranged and Deflect Summoning.
        /// </summary>
        DeflectRangeSummoning = 17,
        /// <summary>
        /// A combined icon for Deflect Magic and Deflect Summoning.
        /// </summary>
        DeflectMageSummoning = 18,
        /// <summary>
        /// The icon for the Wrath curse.
        /// </summary>
        Wrath = 19,
        /// <summary>
        /// The icon for the Soul Split curse.
        /// </summary>
        SoulSplit = 20,
        /// <summary>
        /// The red skull icon, indicating a player has attacked another player in the Wilderness.
        /// </summary>
        RedSkull = 21,
        /// <summary>
        /// A combined icon for Melee and Ranged protection.
        /// </summary>
        MeleeRange = 22,
        /// <summary>
        /// A combined icon for Melee and Magic protection.
        /// </summary>
        MeleeMage = 23,
        /// <summary>
        /// A combined icon for Melee, Magic, and Ranged protection.
        /// </summary>
        MeleeMageRange = 24,
    }
}
