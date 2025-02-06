namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Contains curse prayer type.
    /// </summary>
    public enum AncientCurses : int
    {
        /// <summary>
        /// 
        /// </summary>
        ProtectItem = 0 | (1 << 8) | 0x4000, // first group
        /// <summary>
        /// 
        /// </summary>
        SapWarrior = 1 | (2 << 8) | (5 << 16) | (6 << 20) | 0x4000, // second group , deactivates 5th and 6th group
        /// <summary>
        /// 
        /// </summary>
        SapRanger = 2 | (2 << 8) | (5 << 16) | (6 << 20) | 0x4000, // second group , deactivates 5th and 6th group
        /// <summary>
        /// 
        /// </summary>
        SapMage = 3 | (2 << 8) | (5 << 16) | (6 << 20) | 0x4000, // second group , deactivates 5th and 6th group
        /// <summary>
        /// 
        /// </summary>
        SapSpirit = 4 | (2 << 8) | (5 << 16) | (6 << 20) | 0x4000, // second group , deactivates 5th and 6th group

        /// <summary>
        /// 
        /// </summary>
        Berserker = 5 | (3 << 8) | 0x4000, // third group
        /// <summary>
        /// 
        /// </summary>
        DeflectSummoning = 6 | (7 << 8) | (8 << 16) | 0x4000, // 7th group , deactivates 8th group
        /// <summary>
        /// 
        /// </summary>
        DeflectMagic = 7 | (4 << 8) | (4 << 16) | (8 << 20) | 0x4000, // fourth group , deactivates 4th group and 8th group
        /// <summary>
        /// 
        /// </summary>
        DeflectMissiles = 8 | (4 << 8) | (4 << 16) | (8 << 20) | 0x4000, // fourth group , deactivates 4th group and 8th group
        /// <summary>
        /// 
        /// </summary>
        DeflectMelee = 9 | (4 << 8) | (4 << 16) | (8 << 20) | 0x4000, // fourth group , deactivates 4th group and 8th group

        /// <summary>
        /// 
        /// </summary>
        LeechAttack = 10 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 5th group , deactivates 2th and 6th group
        /// <summary>
        /// 
        /// </summary>
        LeechRanged = 11 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 5th group , deactivates 2th and 6th group
        /// <summary>
        /// 
        /// </summary>
        LeechMagic = 12 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 5th group , deactivates 2th and 6th group
        /// <summary>
        /// 
        /// </summary>
        LeechDefence = 13 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 5th group , deactivates 2th and 6th group
        /// <summary>
        /// 
        /// </summary>
        LeechStrength = 14 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 5th group , deactivates 2th and 6th group

        /// <summary>
        /// 
        /// </summary>
        LeechEnergy = 15 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 2th group , deactivates 2th and 6th group
        /// <summary>
        /// 
        /// </summary>
        LeechSpecialAttack = 16 | (5 << 8) | (2 << 16) | (6 << 20) | 0x4000, // 2th group , deactivates 2th and 6th group
        /// <summary>
        /// 
        /// </summary>
        Wrath = 17 | (8 << 8) | (4 << 16) | (7 << 20) | (8 << 24) | 0x4000, // 8th group , deactivates 4th group , 7th group and 8th group
        /// <summary>
        /// 
        /// </summary>
        SoulSplit = 18 | (8 << 8) | (4 << 16) | (7 << 20) | (8 << 24) | 0x4000, // 8th group , deactivates 4th group , 7th group and 8th group
        /// <summary>
        /// 
        /// </summary>
        Turmoil = 19 | (6 << 8) | (2 << 16) | (5 << 20) | 0x4000, // 6th group , deactivates second and 5th group.
    }
}
