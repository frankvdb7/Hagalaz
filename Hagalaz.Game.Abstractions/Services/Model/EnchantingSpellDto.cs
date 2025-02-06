using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class EnchantingSpellDto
    {

        /// <summary>
        /// The child identifier.
        /// </summary>
        public required int ButtonId { get; init; }

        /// <summary>
        /// The required runes
        /// </summary>
        public required RuneType[] RequiredRunes { get; init; }

        /// <summary>
        /// The required runes amounts
        /// </summary>
        public required int[] RequiredRunesCounts { get; init; }

        /// <summary>
        /// The required level
        /// </summary>
        public required int RequiredLevel { get; init; }

        /// <summary>
        /// The experience
        /// </summary>
        public required double Experience
        {
            get; init;
        }

        /// <summary>
        /// The graphics identifier
        /// </summary>
        public required int GraphicId
        {
            get; init;
        }
    }
}