using System;
using System.Threading.Tasks;

namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Represents a game task that wraps an asynchronous operation (<see cref="Task"/>) without a return value.
    /// This allows a standard C# Task to be managed by the game's tick-based scheduler.
    /// </summary>
    public class RsAsyncTask : ITaskItem, IDisposable
    {
        private Func<Task>? _taskExec;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsAsyncTask"/> class.
        /// </summary>
        /// <param name="taskExec">The asynchronous delegate to execute.</param>
        public RsAsyncTask(Func<Task> taskExec) => _taskExec = taskExec;

        /// <inheritdoc />
        public bool IsCancelled { get; private set; }

        /// <inheritdoc />
        public bool IsCompleted { get; private set; }

        /// <inheritdoc />
        public bool IsFaulted { get; private set; }

        /// <inheritdoc />
        public void Tick()
        {
            if (IsCancelled || IsCompleted || IsFaulted)
            {
                return;
            }

            try
            {
                var task = _taskExec?.Invoke();
                task?.ConfigureAwait(false).GetAwaiter().GetResult();
                IsCompleted = task?.IsCompleted ?? true;
            }
            catch
            {
                IsFaulted = true;
                throw;
            }
        }

        private void ReleaseUnmanagedResources() => _taskExec = null;

        /// <inheritdoc />
        public void Cancel() => IsCancelled = true;

        /// <summary>
        /// Releases the resources used by the task.
        /// </summary>
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="RsAsyncTask"/> class.
        /// </summary>
        ~RsAsyncTask() => ReleaseUnmanagedResources();
    }
}
