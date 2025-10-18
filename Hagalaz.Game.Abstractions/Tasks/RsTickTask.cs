using System;

namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Represents a task that executes a specific action on every game tick until it is explicitly canceled.
    /// </summary>
    public class RsTickTask : ITaskItem, IDisposable
    {
        /// <summary>
        /// The action to be invoked on each tick.
        /// </summary>
        protected Action? TickActionMethod;

        /// <summary>
        /// Gets the number of times this task has been ticked.
        /// </summary>
        public int TickCount { get; protected set; }

        /// <inheritdoc />
        public bool IsCancelled { get; private set; }

        /// <inheritdoc />
        public bool IsFaulted { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the task has completed. For a tick task, completion is equivalent to cancellation.
        /// </summary>
        public bool IsCompleted => IsCancelled;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsTickTask"/> class.
        /// </summary>
        /// <param name="tickAction">The action to execute on each game tick.</param>
        public RsTickTask(Action tickAction) => TickActionMethod = tickAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsTickTask"/> class.
        /// This constructor is intended for use by subclasses that will provide their own tick action logic.
        /// </summary>
        protected RsTickTask() { }

        /// <inheritdoc />
        public void Tick()
        {
            if (IsCancelled || IsCompleted || IsFaulted)
            {
                return;
            }

            try
            {
                TickCount++;
                TickActionMethod?.Invoke();
            }
            catch
            {
                IsFaulted = true;
                throw;
            }
        }

        /// <summary>
        /// Resets the tick counter of this task.
        /// </summary>
        public virtual void Reset() => TickCount = 0;

        /// <inheritdoc />
        public virtual void Cancel() => IsCancelled = true;

        private void ReleaseUnmanagedResources() => TickActionMethod = null;

        /// <summary>
        /// Releases the resources used by the task.
        /// </summary>
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="RsTickTask"/> class.
        /// </summary>
        ~RsTickTask() => ReleaseUnmanagedResources();
    }
}
