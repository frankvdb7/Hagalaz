using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Herblore skill data, such as herb and potion definitions.
    /// </summary>
    public interface IHerbloreService
    {
        /// <summary>
        /// Finds a herb definition by the item ID of its grimy version.
        /// </summary>
        /// <param name="id">The item ID of the grimy herb.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="HerbDto"/> if found; otherwise, <c>null</c>.</returns>
        public Task<HerbDto?> FindGrimyHerbById(int id);

        /// <summary>
        /// Finds all herb definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all herb data transfer objects.</returns>
        public Task<IReadOnlyList<HerbDto>> FindAllHerbs();

        /// <summary>
        /// Finds all potion definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all potion data transfer objects.</returns>
        Task<IReadOnlyList<PotionDto>> FindAllPotions();
    }
}