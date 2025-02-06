using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IWoodcuttingService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<LogDto>> FindAllLogs();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<TreeDto>> FindAllTrees();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="treeId"></param>
        /// <returns></returns>
        Task<LogDto?> FindLogByTreeId(int treeId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TreeDto?> FindTreeById(int id);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<HatchetDto>> FindAllHatchets();
    }
}