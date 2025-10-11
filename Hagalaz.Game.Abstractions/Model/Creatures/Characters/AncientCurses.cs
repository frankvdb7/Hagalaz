namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the set of Ancient Curses, a high-level prayer book. The integer values use bitwise operations to manage activation groups.
    /// </summary>
    public enum AncientCurses : int
    {
        /// <summary>
        /// Protects one additional item upon death.
        /// </summary>
        ProtectItem = 0 | (1 << 8) | 0x4000, // first group
        /// <summary>
        /// Saps opponent's Attack, Strength, and Defence.
        /// </summary>
        SapWarrior = 1 | (2 << 8) | (5 << 16) | (6 << 20) | 0x4000, // second group , deactivates 5th and 6th group
        /// <summary>
        /// Saps opponent's Ranged and Defence.
        /// </summary>
        SapRanger = 2 | (2 << 8) | (5 << 16) | (6 << 20) | 0x4000, // second group , deactivates 5th and 6th group
        /// <summary>
        /// Saps opponent's Magic and Defence.
        /// </summary>
        SapMage = 3 | (2 << 8) | (5 << 16) | (6 << 20) | 0x4000, // second group , deactivates 5th and 6th group
        /// <summary>
        /// Saps opponent's special attack energy.
        /// </summary>
        SapSpirit = 4 | (2 << 8) | (5 << 16) | (6 << 20) | 0x4000, // second group , deactivates 5th and 6th group
        /// <summary>
        /// Increases Strength and Defence, but lowers Attack.
        /// </summary>
        Berserker = 5 | (3 << 8) | 0x4000, // third group
        /// <summary>
        /// Deflects a portion of incoming Summoning damage back to the attacker.
        /// </summary>
        DeflectSummoning = 6 | (7 << 8) | (8 << 16) | 0x4000, // 7th group , deactivates 8th group
        /// <summary>
        /// Deflects a portion of incoming Magic damage back to the attacker.
        /// </summary>
        DeflectMagic = 7 | (4 << 8) | (4 << 16) | (8 << 20) | 0x4000, // fourth group , deactivates 4th group and 8th group
        /// <summary>
        /// Deflects a portion of incoming Ranged damage back to the attacker.
        /// </summary>
        DeflectMissiles = 8 | (4 << 8) | (4 << 16) | (8 << 20) | 0x4000, // fourth group , deactivates 4th group and 8th group
        /// <summary>
        /// Deflects a portion of incoming Melee damage back to the attacker.
        /// </summary>
        DeflectMelee = 9 | (4 << 8) | (4 << 16) | (8 << 20) | 0x4000, // fourth group , deactivates 4th group and 8th group
        /// <summary>
        /// Heals the user by a fraction of the melee damage dealt while draining the opponent's Attack.
        /// </summary>
        LeechAttack = 10 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 5th group , deactivates 2th and 6th group
        /// <summary>
        /// Heals the user by a fraction of the ranged damage dealt while draining the opponent's Ranged.
        /// </summary>
        LeechRanged = 11 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 5th group , deactivates 2th and 6th group
        /// <summary>
        /// Heals the user by a fraction of the magic damage dealt while draining the opponent's Magic.
        /// </summary>
        LeechMagic = 12 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 5th group , deactivates 2th and 6th group
        /// <summary>
        /// Drains the opponent's Defence.
        /// </summary>
        LeechDefence = 13 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 5th group , deactivates 2th and 6th group
        /// <summary>
        /// Heals the user by a fraction of the melee damage dealt while draining the opponent's Strength.
        /// </summary>
        LeechStrength = 14 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 5th group , deactivates 2th and 6th group
        /// <summary>
        /// Restores run energy while active.
        /// </summary>
        LeechEnergy = 15 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 2th group , deactivates 2th and 6th group
        /// <summary>
        /// Restores special attack energy.
        /// </summary>
        LeechSpecialAttack = 16 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 2th group , deactivates 2th and 6th group
        /// <summary>
        /// Deals damage to nearby enemies upon the user's death.
        /// </summary>
        Wrath = 17 | (8 << 8) | (4 << 16) | (7 << 20) | (8 << 24) | 0x4000, // 8th group , deactivates 4th group , 7th group and 8th group
        /// <summary>
        /// Heals the user for a portion of the damage they deal, while also damaging their prayer points.
        /// </summary>
        SoulSplit = 18 | (8 << 8) | (4 << 16) | (7 << 20) | (8 << 24) | 0x4000, // 8th group , deactivates 4th group , 7th group and 8th group
        /// <summary>
        /// Greatly increases Attack, Strength, and Defence while draining the opponent's corresponding stats.
        /// </summary>
        Turmoil = 19 | (6 << 8) | (2 << 16) | (5 << 20) | 0x4000, // 6th group , deactivates second and 5th group.
    }
}
