using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public record CombatSpellDto
    {
        /// <summary>
        /// The child identifier.
        /// </summary>
        public int ButtonId { get; init; }

        /// <summary>
        /// The base damage.
        /// </summary>
        public int BaseDamage { get; init; }

        /// <summary>
        /// The required level
        /// </summary>
        public int RequiredLevel { get; init; }

        /// <summary>
        /// The required runes
        /// </summary>
        public RuneType[] RequiredRunes { get; set; } = default!;

        /// <summary>
        /// The required runes amounts
        /// </summary>
        public int[] RequiredRunesCounts { get; set; } = default!;

        /// <summary>
        /// The experience
        /// </summary>
        public double BaseExperience { get; init; }

        /// <summary>
        /// The cast animation identifier.
        /// </summary>
        public int CastAnimationId { get; set; }

        /// <summary>
        /// The cast graphic identifier.
        /// </summary>
        public int CastGraphicId { get; set; }

        /// <summary>
        /// The end graphic identifier.
        /// </summary>
        public int EndGraphicId { get; set; }

        /// <summary>
        /// The projectile identifier.
        /// </summary>
        public int ProjectileId { get; set; }

        /// <summary>
        /// The automatic cast configuration.
        /// </summary>
        public int AutoCastConfig { get; set; }
    }
}