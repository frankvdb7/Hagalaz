namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Represents the outcome of an attack, detailing the damage inflicted and its effect on the target's life points.
    /// </summary>
    public record AttackResult
    {
        /// <summary>
        /// Represents the result of an attack in terms of whether it inflicted damage and the amount of damage caused.
        /// The `Succeeded` property indicates if the attack successfully dealt damage.
        /// The `Count` property represents the damage amount inflicted by the attack.
        /// </summary>
        public required (bool Succeeded, int Count) Damage { get; init; }

        /// <summary>
        /// Indicates the result of an attack in terms of life points impact.
        /// The `Succeeded` property shows if the attack successfully affected life points.
        /// The `Count` property represents the number of life points affected by the attack.
        /// </summary>
        public required (bool Succeeded, int Count) DamageLifePoints { get; init; }
    }
}