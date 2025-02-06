using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICharacterLocationService
    {
        /// <summary>
        /// Sets the character location.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="location">The location.</param>
        void SetLocationByIndex(int index, ILocation location);
        /// <summary>
        /// Gets the character location.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        ILocation FindLocationByIndex(int index);
    }
}
