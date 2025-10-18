using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages map areas.
    /// </summary>
    public interface IAreaService
    {
        /// <summary>
        /// Finds the map area that contains a specific location.
        /// </summary>
        /// <param name="location">The location to find the area for.</param>
        /// <returns>The <see cref="IArea"/> containing the location.</returns>
        IArea FindAreaByLocation(ILocation location);
    }
}