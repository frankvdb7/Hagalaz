namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the types of combat bonuses that can be granted by prayers or curses.
    /// </summary>
    public enum BonusPrayerType : byte
    {
        /// <summary>
        /// A static bonus to Attack level from standard prayers.
        /// </summary>
        StaticAttack = 0,
        /// <summary>
        /// A static bonus to Strength level from standard prayers.
        /// </summary>
        StaticStrength = 1,
        /// <summary>
        /// A static bonus to Defence level from standard prayers.
        /// </summary>
        StaticDefence = 2,
        /// <summary>
        /// A static bonus to Ranged level from standard prayers.
        /// </summary>
        StaticRanged = 3,
        /// <summary>
        /// A static bonus to Magic level from standard prayers.
        /// </summary>
        StaticMagic = 4,
        /// <summary>
        /// An Attack level bonus from curses, which may have dynamic effects.
        /// </summary>
        CurseAttack = 5,
        /// <summary>
        /// A Strength level bonus from curses.
        /// </summary>
        CurseStrength = 6,
        /// <summary>
        /// A Defence level bonus from curses.
        /// </summary>
        CurseDefence = 7,
        /// <summary>
        /// A Ranged level bonus from curses.
        /// </summary>
        CurseRanged = 8,
        /// <summary>
        /// A Magic level bonus from curses.
        /// </summary>
        CurseMagic = 9,
        /// <summary>
        /// An Attack level bonus from the Turmoil curse, which also drains opponent stats.
        /// </summary>
        TurmoilAttack = 10,
        /// <summary>
        /// A Strength level bonus from the Turmoil curse.
        /// </summary>
        TurmoilStrength = 11,
        /// <summary>
        /// A Defence level bonus from the Turmoil curse.
        /// </summary>
        TurmoilDefence = 12,
        /// <summary>
        /// An instantaneous Attack level bonus from a curse effect.
        /// </summary>
        CurseInstantAttack = 13,
        /// <summary>
        /// An instantaneous Strength level bonus from a curse effect.
        /// </summary>
        CurseInstantStrength = 14,
        /// <summary>
        /// An instantaneous Defence level bonus from a curse effect.
        /// </summary>
        CurseInstantDefence = 15,
        /// <summary>
        /// An instantaneous Ranged level bonus from a curse effect.
        /// </summary>
        CurseInstantRanged = 16,
        /// <summary>
        /// An instantaneous Magic level bonus from a curse effect.
        /// </summary>
        CurseInstantMagic = 17,
    }
}
