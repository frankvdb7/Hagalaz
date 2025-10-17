using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that tracks the locations of characters.
    /// </summary>
    public interface ICharacterLocationService
    {
        /// <summary>
        /// Sets the location for a character identified by their index.
        /// </summary>
        /// <param name="index">The index of the character.</param>
        /// <param name="location">The new location of the character.</param>
        void SetLocationByIndex(int index, ILocation location);

        /// <summary>
        /// Finds the location of a character by their index.
        /// </summary>
        /// <param name="index">The index of the character.</param>
        /// <returns>The <see cref="ILocation"/> of the character.</returns>
        ILocation FindLocationByIndex(int index);
    }
}