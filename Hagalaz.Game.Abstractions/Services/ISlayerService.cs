using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Slayer skill data, such as task and master definitions.
    /// </summary>
    public interface ISlayerService
    {
        /// <summary>
        /// Finds a Slayer task definition by its ID.
        /// </summary>
        /// <param name="taskID">The ID of the Slayer task.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ISlayerTaskDefinition"/> if found; otherwise, <c>null</c>.</returns>
        Task<ISlayerTaskDefinition?> FindSlayerTaskDefinition(int taskID, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds a Slayer master definition by their NPC ID.
        /// </summary>
        /// <param name="npcId">The ID of the Slayer master NPC.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="ISlayerMasterTable"/> if found; otherwise, <c>null</c>.</returns>
        Task<ISlayerMasterTable?> FindSlayerMasterTableByNpcId(int npcId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds all Slayer master definitions.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all Slayer master tables.</returns>
        Task<IReadOnlyList<ISlayerMasterTable>> FindAllSlayerMasterTables(CancellationToken cancellationToken = default);
    }
}