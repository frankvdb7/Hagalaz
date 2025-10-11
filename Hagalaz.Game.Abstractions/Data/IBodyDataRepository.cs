using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Data
{
    /// <summary>
    /// Defines a contract for a repository that provides data related to character body parts and appearance slots.
    /// </summary>
    public interface IBodyDataRepository
    {
        /// <summary>
        /// Determines whether a specific body part slot is disabled or unavailable for customization.
        /// </summary>
        /// <param name="part">The body part to check.</param>
        /// <returns><c>true</c> if the specified body part slot is disabled; otherwise, <c>false</c>.</returns>
        bool IsDisabledSlot(BodyPart part);

        /// <summary>
        /// Gets the total number of available body slots for character appearance customization.
        /// </summary>
        int BodySlotCount { get; }
    }
}