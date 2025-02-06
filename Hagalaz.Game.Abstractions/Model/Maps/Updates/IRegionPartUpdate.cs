using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Model.Maps.Updates
{
    /// <summary>
    /// Interface for zone updates.
    /// </summary>
    public interface IRegionPartUpdate
    {
        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        ILocation Location { get; }
        /// <summary>
        /// Determines whether this instance [can update for] the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can update for] the specified character; otherwise, <c>false</c>.
        /// </returns>
        bool CanUpdateFor(ICharacter character);
        /// <summary>
        /// Called when the update is sent to the character.
        /// </summary>
        /// <param name="character">The character.</param>
        void OnUpdatedFor(ICharacter character);
    }
}
