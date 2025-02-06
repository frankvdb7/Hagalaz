using System.Collections.Generic;
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
        /// <returns></returns>
        Task<ISlayerTaskDefinition?> FindSlayerTaskDefinition(int taskID);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="npcId"></param>
        /// <returns></returns>
        Task<ISlayerMasterTable?> FindSlayerMasterTableByNpcId(int npcId);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<ISlayerMasterTable>> FindAllSlayerMasterTables();
    }
}