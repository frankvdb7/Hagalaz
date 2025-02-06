using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class CharacterLocationService : ICharacterLocationService
    {
        /// <summary>
        /// Get's location map
        /// </summary>
        private readonly ILocation?[] _characterLocations;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterLocationService"/> class.
        /// </summary>
        public CharacterLocationService() => _characterLocations = new ILocation[2048];

        /// <summary>
        /// Sets the character location.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="location">The location.</param>
        public void SetLocationByIndex(int index, ILocation location) => _characterLocations[index] = location;

        /// <summary>
        /// Gets the character location.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public ILocation FindLocationByIndex(int index) => _characterLocations[index] ?? Location.Zero;
    }
}