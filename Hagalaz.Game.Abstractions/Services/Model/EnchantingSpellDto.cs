using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// A data transfer object containing the definition of an enchanting spell.
    /// </summary>
    public class EnchantingSpellDto
    {
        /// <summary>
        /// Gets the widget button ID for this spell.
        /// </summary>
        public required int ButtonId { get; init; }

        /// <summary>
        /// Gets the array of rune types required to cast this spell.
        /// </summary>
        public required RuneType[] RequiredRunes { get; init; }

        /// <summary>
        /// Gets the array of rune quantities required to cast this spell.
        /// </summary>
        public required int[] RequiredRunesCounts { get; init; }

        /// <summary>
        /// Gets the required Magic level to cast this spell.
        /// </summary>
        public required int RequiredLevel { get; init; }

        /// <summary>
        /// Gets the Magic experience gained upon casting this spell.
        /// </summary>
        public required double Experience { get; init; }

        /// <summary>
        /// Gets the ID of the graphic effect for this spell.
        /// </summary>
        public required int GraphicId { get; init; }
    }
}