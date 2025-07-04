using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Represents parameters for defining an attack in the game.
    /// </summary>
    /// <remarks>
    /// This record is used to encapsulate details related to an attack performed by a creature,
    /// such as the target of the attack, damage type, damage amount, optional maximum damage,
    /// and the delay before the attack occurs.
    /// </remarks>
    public record AttackParams
    {
        /// <summary>
        /// Specifies the target of an attack operation. The target is an entity that implements
        /// the <see cref="ICreature"/> interface and represents the creature or entity
        /// that will receive the attack within the game mechanics.
        /// </summary>
        public required ICreature Target { get; init; }
        /// <summary>
        /// Indicates the type of damage associated with an attack. This property defines the category
        /// of the inflicted damage, which determines how the attack is processed and mitigated
        /// within the combat mechanics.
        /// </summary>
        public required DamageType DamageType { get; init; }
        /// <summary>
        /// Indicates the type of hit resulting from an attack operation. This property
        /// determines the visual and logical representation of the hit, such as whether
        /// it is a melee, ranged, or magic hit, or a special type like poison or disease.
        /// </summary>
        public HitSplatType? HitType { get; init; }
        /// <summary>
        /// Represents the amount of damage to be inflicted upon the target during an attack.
        /// This value is calculated based on combat mechanics and varies according to
        /// the attacker's attributes, weapon, and context of the attack.
        /// </summary>
        public required int Damage { get; init; }
        /// <summary>
        /// Represents the maximum potential damage that an attack can cause to a target.
        /// This value is determined based on various factors such as the character's stats,
        /// equipment, and abilities within the combat system mechanics.
        /// </summary>
        public int? MaxDamage { get; init; }
        /// <summary>
        /// Represents the delay, in ticks, before the effects of an attack take place or are registered
        /// within the game mechanics. This period accounts for travel time or preparation time
        /// associated with the attack.
        /// </summary>
        public int Delay { get; init; }
    }
}