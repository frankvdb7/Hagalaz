using System;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISystemUpdateService
    {
        /// <summary>
        /// 
        /// </summary>
        bool SystemUpdateScheduled { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="executionTime"></param>
        void ScheduleUpdate(DateTimeOffset executionTime);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tickTime"></param>
        void ScheduleUpdate(int tickTime);

        /// <summary>
        /// 
        /// </summary>
        void CancelUpdate();
    }
}