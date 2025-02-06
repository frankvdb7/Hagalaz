namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    public record AttackResult
    {
        /// <summary>
        /// This damage calculated after the incoming attack.
        /// This checks for protection prayers and other calculations before subtracting life points.
        /// If not succeeded, it means that attack wasn't performed.
        /// </summary>
        public required (bool Succeeded, int Count) Damage { get; init; }
        /// <summary>
        /// This damage subtracted from the life points of the target.
        /// If not succeeded, it means that attack wasn't performed.
        /// </summary>
        public required (bool Succeeded, int Count) DamageLifePoints { get; init; }
    }
}