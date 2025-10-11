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
        ActiveMelle = 0,
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
        ActiveDungeonerring = 9,
        /// <summary>
        /// An active hitsplat for a maximum melee hit.
        /// </summary>
        ActiveMaxedMelle = 10,
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
        PasiveMelleDamage = 14,
        /// <summary>
        /// A passive hitsplat for ranged damage.
        /// </summary>
        PasiveRangeDamage = 15,
        /// <summary>
        /// A passive hitsplat for magic damage.
        /// </summary>
        PasiveMageDamage = 16,
        /// <summary>
        /// A generic passive red hitsplat.
        /// </summary>
        PasiveRedDamage = 17,
        /// <summary>
        /// A passive hitsplat indicating deflected damage.
        /// </summary>
        PasiveDeflectDamage = 18,
        /// <summary>
        /// A passive hitsplat indicating defended (blocked) damage.
        /// </summary>
        PasiveDefendedDamage = 19,
        /// <summary>
        /// A passive hitsplat for poison damage.
        /// </summary>
        PasivePoisonDamage = 20,
        /// <summary>
        /// A secondary passive hitsplat for poison damage.
        /// </summary>
        PasivePoisonDamage2 = 21,
        /// <summary>
        /// An empty or transparent hitsplat, often used as a placeholder.
        /// </summary>
        EmptyDamage = 22,
        /// <summary>
        /// A passive hitsplat for Dungeoneering-specific damage.
        /// </summary>
        PasiveDungeonerringDamage = 23,
        /// <summary>
        /// A passive hitsplat for a maximum melee hit.
        /// </summary>
        PasiveMaxedMelle = 24,
        /// <summary>
        /// A passive hitsplat for a maximum ranged hit.
        /// </summary>
        PasiveMaxedRange = 25,
        /// <summary>
        /// A passive hitsplat for a maximum magic hit.
        /// </summary>
        PasiveMaxedMage = 26,
        /// <summary>
        /// A passive hitsplat for damage from a dwarf multi-cannon.
        /// </summary>
        PasiveCannonDamage = 27
    }
}