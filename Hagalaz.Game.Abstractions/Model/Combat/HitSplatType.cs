namespace Hagalaz.Game.Abstractions.Model.Combat
{
    /// <summary>
    /// Defines the logical type of a hitsplat, which determines its color and the icon displayed.
    /// </summary>
    public enum HitSplatType
    {
        /// <summary>
        /// A missed hit, typically displayed as a blue 0.
        /// </summary>
        HitMiss = 0,
        /// <summary>
        /// A regular melee damage hit, typically red.
        /// </summary>
        HitMeleeDamage = 1,
        /// <summary>
        /// A regular ranged damage hit, typically green.
        /// </summary>
        HitRangeDamage = 2,
        /// <summary>
        /// A regular magic damage hit, typically blue.
        /// </summary>
        HitMagicDamage = 3,
        /// <summary>
        /// A generic damage hit, often used for typeless damage.
        /// </summary>
        HitSimpleDamage = 4,
        /// <summary>
        /// A poison damage hit, typically green.
        /// </summary>
        HitPoisonDamage = 5,
        /// <summary>
        /// A disease damage hit.
        /// </summary>
        HitDiseaseDamage = 6,
        /// <summary>
        /// A Dungeoneering-specific damage hit.
        /// </summary>
        HitDungeoneeringDamage = 7,
        /// <summary>
        /// Damage that was deflected back to the attacker.
        /// </summary>
        HitDeflectDamage = 8,
        /// <summary>
        /// Damage dealt by a dwarf multi-cannon.
        /// </summary>
        HitCannonDamage = 9,
        /// <summary>
        /// Damage that was blocked or defended against.
        /// </summary>
        HitDefendedDamage = 10,
        /// <summary>
        /// No specific hitsplat type.
        /// </summary>
        None = 11,
    }
}
