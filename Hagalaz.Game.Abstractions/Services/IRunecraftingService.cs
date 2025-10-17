using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Runecrafting skill data, such as altar definitions.
    /// </summary>
    public interface IRunecraftingService
    {
        /// <summary>
        /// Finds all Runecrafting altar definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all Runecrafting data transfer objects.</returns>
        Task<IReadOnlyList<RunecraftingDto>> FindAllAltars();

        /// <summary>
        /// Finds a Runecrafting altar definition by its altar object ID.
        /// </summary>
        /// <param name="altarId">The object ID of the altar.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="RunecraftingDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<RunecraftingDto?> FindAltarById(int altarId);

        /// <summary>
        /// Finds a Runecrafting altar definition by the object ID of its ruins.
        /// </summary>
        /// <param name="ruinId">The object ID of the ruins.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="RunecraftingDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<RunecraftingDto?> FindAltarByRuinId(int ruinId);

        /// <summary>
        /// Finds a Runecrafting altar definition by the object ID of its rift.
        /// </summary>
        /// <param name="riftId">The object ID of the rift.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="RunecraftingDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<RunecraftingDto?> FindAltarByRiftId(int riftId);

        /// <summary>
        /// Finds a Runecrafting altar definition by the object ID of its portal.
        /// </summary>
        /// <param name="portalId">The object ID of the portal.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="RunecraftingDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<RunecraftingDto?> FindAltarByPortalId(int portalId);
    }
}