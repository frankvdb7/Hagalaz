using System;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages scheduled system updates.
    /// </summary>
    public interface ISystemUpdateService
    {
        /// <summary>
        /// Gets a value indicating whether a system update is currently scheduled.
        /// </summary>
        bool SystemUpdateScheduled { get; }

        /// <summary>
        /// Schedules a system update for a specific time.
        /// </summary>
        /// <param name="executionTime">The time at which the update should occur.</param>
        void ScheduleUpdate(DateTimeOffset executionTime);

        /// <summary>
        /// Schedules a system update to occur after a specific number of game ticks.
        /// </summary>
        /// <param name="tickTime">The number of ticks to wait before the update.</param>
        void ScheduleUpdate(int tickTime);

        /// <summary>
        /// Cancels any currently scheduled system update.
        /// </summary>
        void CancelUpdate();
    }
}