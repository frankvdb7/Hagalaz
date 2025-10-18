namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Defines a contract for a scheduler that processes a queue of actions on each game tick.
    /// </summary>
    /// <typeparam name="TScheduleType">The type of the actions or tasks to be scheduled.</typeparam>
    public interface IScheduler<in TScheduleType> : ITickItem
    {
        /// <summary>
        /// Schedules a new action to be executed by the scheduler.
        /// </summary>
        /// <param name="action">The action to schedule.</param>
        void Schedule(TScheduleType action);
    }
}