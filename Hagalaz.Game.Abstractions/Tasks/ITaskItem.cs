using System;

namespace Hagalaz.Game.Abstractions.Tasks
{
    public interface ITaskItem<out TResult> : ITaskItem
    {
        public void RegisterResultHandler(Action<TResult> resultHandler);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ITaskItem : ITickItem
    {
        /// <summary>
        /// Whether the task is canceled.
        /// </summary>
        bool IsCancelled { get; }
        /// <summary>
        /// Whether the task is completed.
        /// </summary>
        bool IsCompleted { get; }
        /// <summary>
        /// Whether the task is faulted.
        /// </summary>
        bool IsFaulted { get; }
        /// <summary>
        /// Call to cancel the task.
        /// </summary>
        void Cancel();
    }
}