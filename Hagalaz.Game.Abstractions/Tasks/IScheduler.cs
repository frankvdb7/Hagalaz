namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TScheduleType">The type of the schedule.</typeparam>
    public interface IScheduler<in TScheduleType> : ITickItem
    {
        /// <summary>
        /// Schedules the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        void Schedule(TScheduleType action);
    }
}