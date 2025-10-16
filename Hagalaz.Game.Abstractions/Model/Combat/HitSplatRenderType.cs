namespace Hagalaz.Game.Abstractions.Model.Combat
{
    /// <summary>
    /// Defines the visual rendering type of a hitsplat, distinguishing between active (primary) and passive (secondary) hits.
    /// </summary>
    public enum HitSplatRenderType
    {
        /// <summary>
        /// An active hitsplat for melee damage.
        /// </summary>
        ActiveMelee = 0,
        /// <summary>
        /// An active hitsplat for ranged damage.
        /// </summary>
        ActiveRange = 1,
        /// <summary>
        /// An active hitsplat for magic damage.
        /// </summary>
        ActiveMage = 2,
        /// <summary>
        /// A generic active red hitsplat.
        /// </summary>
        ActiveRed = 3,
        /// <summary>
        /// An active hitsplat indicating deflected damage.
        /// </summary>
        ActiveDeflect = 4,
        /// <summary>
        /// An active hitsplat indicating defended (blocked) damage.
        /// </summary>
        ActiveDefended = 5,
        /// <summary>
        /// An active hitsplat for poison damage.
        /// </summary>
        ActivePoison = 6,
        /// <summary>
        /// An active hitsplat for disease damage.
        /// </summary>
        ActiveDisease = 7,
        /// <summary>
        /// A hitsplat indicating a missed attack (a "0").
        /// </summary>
        Miss = 8,
        /// <summary>
        /// An active hitsplat for Dungeoneering-specific damage.
        /// </summary>
        ActiveDungeoneering = 9,
        /// <summary>
        /// An active hitsplat for a maximum melee hit.
        /// </summary>
        ActiveMaxedMelee = 10,
        /// <summary>
        /// An active hitsplat for a maximum ranged hit.
        /// </summary>
        ActiveMaxedRanged = 11,
        /// <summary>
        /// An active hitsplat for a maximum magic hit.
        /// </summary>
        ActiveMaxedMage = 12,
        /// <summary>
        /// An active hitsplat for damage from a dwarf multi-cannon.
        /// </summary>
        ActiveCannonDamage = 13,
        /// <summary>
        /// A passive hitsplat for melee damage.
        /// </summary>
        PassiveMeleeDamage = 14,
        /// <summary>
        /// A passive hitsplat for ranged damage.
        /// </summary>
        PassiveRangeDamage = 15,
        /// <summary>
        /// A passive hitsplat for magic damage.
        /// </summary>
        PassiveMageDamage = 16,
        /// <summary>
        /// A generic passive red hitsplat.
        /// </summary>
        PassiveRedDamage = 17,
        /// <summary>
        /// A passive hitsplat indicating deflected damage.
        /// </summary>
        PassiveDeflectDamage = 18,
        /// <summary>
        /// A passive hitsplat indicating defended (blocked) damage.
        /// </summary>
        PassiveDefendedDamage = 19,
        /// <summary>
        /// A passive hitsplat for poison damage.
        /// </summary>
        PassivePoisonDamage = 20,
        /// <summary>
        /// A secondary passive hitsplat for poison damage.
        /// </summary>
        PassivePoisonDamage2 = 21,
        /// <summary>
        /// An empty or transparent hitsplat, often used as a placeholder.
        /// </summary>
        EmptyDamage = 22,
        /// <summary>
        /// A passive hitsplat for Dungeoneering-specific damage.
        /// </summary>
        PassiveDungeoneeringDamage = 23,
        /// <summary>
        /// A passive hitsplat for a maximum melee hit.
        /// </summary>
        PassiveMaxedMelee = 24,
        /// <summary>
        /// A passive hitsplat for a maximum ranged hit.
        /// </summary>
        PassiveMaxedRange = 25,
        /// <summary>
        /// A passive hitsplat for a maximum magic hit.
        /// </summary>
        PassiveMaxedMage = 26,
        /// <summary>
        /// A passive hitsplat for damage from a dwarf multi-cannon.
        /// </summary>
        PassiveCannonDamage = 27
    }
}