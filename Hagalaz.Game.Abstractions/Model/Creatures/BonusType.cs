namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the various types of combat bonuses that can be provided by equipment.
    /// </summary>
    public enum BonusType : byte
    {
        /// <summary>
        /// The attack bonus against stab-vulnerable targets.
        /// </summary>
        AttackStab = 0,
        /// <summary>
        /// The attack bonus against slash-vulnerable targets.
        /// </summary>
        AttackSlash = 1,
        /// <summary>
        /// The attack bonus against crush-vulnerable targets.
        /// </summary>
        AttackCrush = 2,
        /// <summary>
        /// The attack bonus for magic-based attacks.
        /// </summary>
        AttackMagic = 3,
        /// <summary>
        /// The attack bonus for ranged-based attacks.
        /// </summary>
        AttackRanged = 4,
        /// <summary>
        /// The defence bonus against stab-based attacks.
        /// </summary>
        DefenceStab = 5,
        /// <summary>
        /// The defence bonus against slash-based attacks.
        /// </summary>
        DefenceSlash = 6,
        /// <summary>
        /// The defence bonus against crush-based attacks.
        /// </summary>
        DefenceCrush = 7,
        /// <summary>
        /// The defence bonus against magic-based attacks.
        /// </summary>
        DefenceMagic = 8,
        /// <summary>
        /// The defence bonus against ranged-based attacks.
        /// </summary>
        DefenceRanged = 9,
        /// <summary>
        /// The defence bonus against summoning-based attacks.
        /// </summary>
        DefenceSummoning = 10,
        /// <summary>
        /// The percentage of melee damage absorbed.
        /// </summary>
        AbsorbMelee = 11,
        /// <summary>
        /// The percentage of magic damage absorbed.
        /// </summary>
        AbsorbMagic = 12,
        /// <summary>
        /// The percentage of ranged damage absorbed.
        /// </summary>
        AbsorbRange = 13,
        /// <summary>
        /// The bonus to melee strength, increasing maximum hit.
        /// </summary>
        Strength = 14,
        /// <summary>
        /// The bonus to ranged strength, increasing maximum hit.
        /// </summary>
        RangedStrength = 15,
        /// <summary>
        /// The bonus to the prayer stat, which slows prayer point drain.
        /// </summary>
        Prayer = 16,
        /// <summary>
        /// The percentage bonus to magic damage.
        /// </summary>
        MagicDamage = 17,
    }
}
