using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISlayerService
    {
        /// <summary>
        /// Gets the slayer task definition.
        /// </summary>
        /// <param name="taskID">The task identifier.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ISlayerTaskDefinition?> FindSlayerTaskDefinition(int taskID, CancellationToken cancellationToken = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="npcId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ISlayerMasterTable?> FindSlayerMasterTableByNpcId(int npcId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IReadOnlyList<ISlayerMasterTable>> FindAllSlayerMasterTables(CancellationToken cancellationToken = default);
    }
}