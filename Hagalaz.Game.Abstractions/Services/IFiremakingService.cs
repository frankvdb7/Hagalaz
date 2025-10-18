using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages Firemaking skill data, such as log definitions.
    /// </summary>
    public interface IFiremakingService
    {
        /// <summary>
        /// Finds a firemaking definition by its log item ID.
        /// </summary>
        /// <param name="logId">The item ID of the log.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="FiremakingDto"/> if found; otherwise, <c>null</c>.</returns>
        Task<FiremakingDto?> FindByLogId(int logId);

        /// <summary>
        /// Finds all firemaking log definitions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a read-only list of all firemaking data transfer objects.</returns>
        Task<IReadOnlyList<FiremakingDto>> FindAllLogs();
    }
}