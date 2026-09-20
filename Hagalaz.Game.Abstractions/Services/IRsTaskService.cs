using System;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that schedules and executes general game tasks.
    /// </summary>
    public interface IRsTaskService : IScheduler<ITaskItem>
    {
        /// <summary>
        /// Schedules a lifecycle-critical action that must reach a terminal
        /// outcome across GameWorker shutdown.
        /// </summary>
        void ScheduleLifecycleCritical(Action action);

        /// <summary>
        /// Marks the scheduler as no longer accepting ordinary worker lifetime.
        /// Lifecycle-critical actions remain accepted until shutdown completes.
        /// </summary>
        void BeginShutdown();

        /// <summary>
        /// Completes scheduler shutdown by executing accepted lifecycle-critical
        /// actions without draining ordinary gameplay tasks.
        /// </summary>
        void CompleteShutdown();
    }
}
