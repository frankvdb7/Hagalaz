using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Woodcutting skill data, such as log, tree, and hatchet definitions.
    /// </summary>
    public interface IWoodcuttingService
    {
        /// <summary>
        /// Finds all log definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all log data transfer objects.</returns>
        Task<IReadOnlyList<LogDto>> FindAllLogs();

        /// <summary>
        /// Finds all tree definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all tree data transfer objects.</returns>
        Task<IReadOnlyList<TreeDto>> FindAllTrees();

        /// <summary>
        /// Finds a log definition by its corresponding tree object ID.
        /// </summary>
        /// <param name="treeId">The object ID of the tree.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="LogDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<LogDto?> FindLogByTreeId(int treeId);

        /// <summary>
        /// Finds a tree definition by its object ID.
        /// </summary>
        /// <param name="id">The object ID of the tree.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="TreeDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<TreeDto?> FindTreeById(int id);

        /// <summary>
        /// Finds all hatchet definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all hatchet data transfer objects.</returns>
        Task<IReadOnlyList<HatchetDto>> FindAllHatchets();
    }
}