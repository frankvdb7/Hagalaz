using System;

namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Represents a handle to a game task that returns a result, allowing for cancellation and result handling.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
    public class RsTaskHandle<TResult> : RsTaskHandle, IRsTaskHandle<TResult>
    {
        private readonly ITaskItem<TResult> _taskItemT;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsTaskHandle{TResult}"/> class.
        /// </summary>
        /// <param name="taskItem">The task item this handle controls.</param>
        public RsTaskHandle(ITaskItem<TResult> taskItem) : base(taskItem) => _taskItemT = taskItem;

        /// <inheritdoc />
        public void RegisterResultHandler(Action<TResult> resultHandler) => _taskItemT.RegisterResultHandler(resultHandler);
    }

    /// <summary>
    /// Represents a handle to a game task, providing a way to cancel it.
    /// </summary>
    public class RsTaskHandle : IRsTaskHandle
    {
        private readonly ITaskItem _taskItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsTaskHandle"/> class.
        /// </summary>
        /// <param name="taskItem">The task item this handle controls.</param>
        public RsTaskHandle(ITaskItem taskItem) => _taskItem = taskItem;

        /// <inheritdoc />
        public void Cancel() => _taskItem.Cancel();
    }
}