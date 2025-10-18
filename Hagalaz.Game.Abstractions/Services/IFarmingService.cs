using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Farming skill data, such as patch and seed definitions.
    /// </summary>
    public interface IFarmingService
    {
        /// <summary>
        /// Finds all farming patch definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all patch data transfer objects.</returns>
        Task<IReadOnlyList<PatchDto>> FindAllPatches();

        /// <summary>
        /// Finds a seed definition by its item ID.
        /// </summary>
        /// <param name="itemId">The item ID of the seed.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="SeedDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<SeedDto?> FindSeedById(int itemId);

        /// <summary>
        /// Finds a farming patch definition by its object ID.
        /// </summary>
        /// <param name="objectId">The object ID of the patch.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="PatchDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<PatchDto?> FindPatchById(int objectId);
    }
}