using System;

namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Defines a contract for a game task that is processed over time and returns a result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
    public interface ITaskItem<out TResult> : ITaskItem
    {
        /// <summary>
        /// Registers a callback action to be executed when the task completes and its result is available.
        /// </summary>
        /// <param name="resultHandler">The action to execute with the task's result.</param>
        void RegisterResultHandler(Action<TResult> resultHandler);
    }

    /// <summary>
    /// Defines a contract for a game task that is processed over time by the scheduler.
    /// It inherits from <see cref="ITickItem"/>, indicating it is updated on each game tick.
    /// </summary>
    public interface ITaskItem : ITickItem
    {
        /// <summary>
        /// Gets a value indicating whether the task has been canceled.
        /// </summary>
        bool IsCancelled { get; }

        /// <summary>
        /// Gets a value indicating whether the task has completed its execution.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Gets a value indicating whether the task has terminated due to an unhandled exception.
        /// </summary>
        bool IsFaulted { get; }

        /// <summary>
        /// Signals that the task should be canceled.
        /// </summary>
        void Cancel();
    }
}