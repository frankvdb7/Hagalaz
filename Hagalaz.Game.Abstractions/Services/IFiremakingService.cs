using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFiremakingService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logId"></param>
        /// <returns></returns>
        Task<FiremakingDto?> FindByLogId(int logId);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IReadOnlyList<FiremakingDto>> FindAllLogs();
    }
}