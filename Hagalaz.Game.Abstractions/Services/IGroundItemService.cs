using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages ground items, which are items dropped or placed on the game world map.
    /// </summary>
    public interface IGroundItemService
    {
        /// <summary>
        /// Finds all ground items at a specific location.
        /// </summary>
        /// <param name="location">The location to search for ground items.</param>
        /// <returns>An enumerable collection of all ground items at the specified location.</returns>
        public IEnumerable<IGroundItem> FindByLocation(ILocation location);

        /// <summary>
        /// Finds all ground items at a specific location.
        /// </summary>
        /// <param name="location">The location to search for ground items.</param>
        /// <returns>An enumerable collection of all ground items at the specified location.</returns>
        IEnumerable<IGroundItem> FindAllGroundItems(ILocation location);
    }
}