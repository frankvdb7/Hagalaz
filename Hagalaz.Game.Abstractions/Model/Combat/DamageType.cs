namespace Hagalaz.Game.Abstractions.Model.Combat
{
    /// <summary>
    /// Defines the various types of damage that can be dealt in combat, which affects how protection prayers and other modifiers are applied.
    /// </summary>
    public enum DamageType
    {
        /// <summary>
        /// Melee damage that can be fully blocked by protection prayers. Typically used by standard NPCs.
        /// </summary>
        StandardMelee,
        /// <summary>
        /// Ranged damage that can be fully blocked by protection prayers. Typically used by standard NPCs.
        /// </summary>
        StandardRange,
        /// <summary>
        /// Magic damage that can be fully blocked by protection prayers. Typically used by standard NPCs.
        /// </summary>
        StandardMagic,
        /// <summary>
        /// Summoning-based damage that can be fully blocked by protection prayers.
        /// </summary>
        StandardSummoning,
        /// <summary>
        /// Damage that is reflected back to an attacker (e.g., from a Vengeance spell or Ring of Recoil).
        /// </summary>
        Reflected,
        /// <summary>
        /// Typeless damage that is not reduced by protection prayers.
        /// </summary>
        Standard,
        /// <summary>
        /// Damage from dragonfire, which requires specific anti-dragon protection to mitigate.
        /// </summary>
        DragonFire,
        /// <summary>
        /// Melee damage that is only partially blocked by protection prayers. Typically used by players and more powerful NPCs.
        /// </summary>
        FullMelee,
        /// <summary>
        /// Ranged damage that is only partially blocked by protection prayers. Typically used by players and more powerful NPCs.
        /// </summary>
        FullRange,
        /// <summary>
        /// Magic damage that is only partially blocked by protection prayers. Typically used by players and more powerful NPCs.
        /// </summary>
        FullMagic,
        /// <summary>
        /// Summoning-based damage that is only partially blocked by protection prayers.
        /// </summary>
        FullSummoning,
    }
}
