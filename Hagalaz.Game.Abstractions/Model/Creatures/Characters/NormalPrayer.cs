namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the set of prayers available in the standard prayer book.
    /// </summary>
    public enum NormalPrayer : int
    {
        /// <summary>
        /// Increases Defence by 5%.
        /// </summary>
        ThickSkin = 0 | (1 << 8),
        /// <summary>
        /// Increases Strength by 5%.
        /// </summary>
        BurstOfStrength = 1,
        /// <summary>
        /// Increases Attack by 5%.
        /// </summary>
        ClarityOfThought = 2,
        /// <summary>
        /// Increases Ranged by 5%.
        /// </summary>
        SharpEye = 3,
        /// <summary>
        /// Increases Magic by 5%.
        /// </summary>
        MysticWill = 4,
        /// <summary>
        /// Increases Defence by 10%.
        /// </summary>
        RockSkin = 5,
        /// <summary>
        /// Increases Strength by 10%.
        /// </summary>
        SuperhumanStrength = 6,
        /// <summary>
        /// Increases Attack by 10%.
        /// </summary>
        ImprovedReflexes = 7,
        /// <summary>
        /// Restores stats twice as fast.
        /// </summary>
        RapidRestore = 8,
        /// <summary>
        /// Restores life points over time.
        /// </summary>
        RapidHeal = 9,
        /// <summary>
        /// Protects one additional item upon death.
        /// </summary>
        ProtectItem = 10,
        /// <summary>
        /// Increases Ranged by 10%.
        /// </summary>
        HawkEye = 11,
        /// <summary>
        /// Increases Magic by 10%.
        /// </summary>
        MysticLore = 12,
        /// <summary>
        /// Increases Defence by 15%.
        /// </summary>
        SteelSkin = 13,
        /// <summary>
        /// Increases Strength by 15%.
        /// </summary>
        UltimateStrength = 14,
        /// <summary>
        /// Increases Attack by 15%.
        /// </summary>
        IncredibleReflexes = 15,
        /// <summary>
        /// Provides 40% protection against Summoning attacks.
        /// </summary>
        ProtectFromSummoning = 16,
        /// <summary>
        /// Provides 40% protection against Magic attacks.
        /// </summary>
        ProtectFromMagic = 17,
        /// <summary>
        /// Provides 40% protection against Ranged attacks.
        /// </summary>
        ProtectFromRanged = 18,
        /// <summary>
        /// Provides 40% protection against Melee attacks.
        /// </summary>
        ProtectFromMelee = 19,
        /// <summary>
        /// Increases Ranged by 15%.
        /// </summary>
        EagleEye = 20,
        /// <summary>
        /// Increases Magic by 15%.
        /// </summary>
        MysticMight = 21,
        /// <summary>
        /// Deals damage to nearby enemies upon death.
        /// </summary>
        Retribution = 22,
        /// <summary>
        /// Heals the player when their health drops below 10%.
        /// </summary>
        Redemption = 23,
        /// <summary>
        /// Drains an opponent's prayer points by 1/4 of the damage dealt.
        /// </summary>
        Smite = 24,
        /// <summary>
        /// Increases Attack by 15%, Strength by 18%, and Defence by 20%.
        /// </summary>
        Chivalry = 25,
        /// <summary>
        /// Greatly increases life point regeneration.
        /// </summary>
        RapidRenewal = 26,
        /// <summary>
        /// Increases Attack by 20%, Strength by 23%, and Defence by 25%.
        /// </summary>
        Piety = 27,
        /// <summary>
        /// Increases Ranged by 20% and Defence by 25%.
        /// </summary>
        Rigour = 28,
        /// <summary>
        /// Increases Magic by 20% and Defence by 25%.
        /// </summary>
        Augury = 29,
    }
}
