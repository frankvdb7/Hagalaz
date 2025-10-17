using System;

namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Defines a contract for a handle to a game task that returns a result.
    /// It allows for registering a callback to handle the result once the task is complete.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
    public interface IRsTaskHandle<out TResult> : IRsTaskHandle
    {
        /// <summary>
        /// Registers a callback action to be executed when the task completes and its result is available.
        /// </summary>
        /// <param name="resultHandler">The action to execute with the task's result.</param>
        void RegisterResultHandler(Action<TResult> resultHandler);
    }

    /// <summary>
    /// Defines a contract for a handle to a game task, providing a mechanism to cancel its execution.
    /// </summary>
    public interface IRsTaskHandle
    {
        /// <summary>
        /// Cancels the execution of the task.
        /// </summary>
        void Cancel();
    }
}