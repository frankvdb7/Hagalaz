using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of a combat spell.
    /// </summary>
    public record CombatSpellDto
    {
        /// <summary>
        /// Gets the widget button ID for this spell.
        /// </summary>
        public int ButtonId { get; init; }

        /// <summary>
        /// Gets the base damage of the spell.
        /// </summary>
        public int BaseDamage { get; init; }

        /// <summary>
        /// Gets the required Magic level to cast this spell.
        /// </summary>
        public int RequiredLevel { get; init; }

        /// <summary>
        /// Gets or sets the array of rune types required to cast this spell.
        /// </summary>
        public RuneType[] RequiredRunes { get; set; } = default!;

        /// <summary>
        /// Gets or sets the array of rune quantities required to cast this spell.
        /// </summary>
        public int[] RequiredRunesCounts { get; set; } = default!;

        /// <summary>
        /// Gets the base Magic experience gained upon casting this spell.
        /// </summary>
        public double BaseExperience { get; init; }

        /// <summary>
        /// Gets or sets the ID of the casting animation.
        /// </summary>
        public int CastAnimationId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the casting graphic effect.
        /// </summary>
        public int CastGraphicId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the graphic effect that plays on the target.
        /// </summary>
        public int EndGraphicId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the projectile graphic.
        /// </summary>
        public int ProjectileId { get; set; }

        /// <summary>
        /// Gets or sets the client configuration ID for autocasting this spell.
        /// </summary>
        public int AutoCastConfig { get; set; }
    }
}