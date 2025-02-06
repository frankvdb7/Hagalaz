using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hagalaz.Services.GameWorld.Services.Model
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISystemUpdateTask
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="executionTime"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ExecuteAsync(DateTimeOffset executionTime, CancellationToken cancellationToken);
    }
}