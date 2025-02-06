using System;

namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Task that runs until cancelled.
    /// </summary>
    public class RsTickTask : ITaskItem, IDisposable
    {
        /// <summary>
        /// Contains action perform method.
        /// </summary>
        protected Action? TickActionMethod;

        /// <summary>
        /// Contains perform times counter.
        /// </summary>
        /// <value>The times performed.</value>
        public int TickCount { get; protected set; }
        /// <summary>
        /// Contains whether this task is cancelled.
        /// </summary>
        public bool IsCancelled { get; private set; }
        /// <summary>
        /// Whether this task is faulted.
        /// </summary>
        public bool IsFaulted { get; private set; }

        /// <summary>
        /// Contains whether this task is completed.
        /// </summary>
        public bool IsCompleted => IsCancelled;

        /// <summary>
        /// Creates new action.
        /// </summary>
        /// <param name="tickAction">The perform method.</param>
        public RsTickTask(Action tickAction) => TickActionMethod = tickAction;

        /// <summary>
        /// Creates new tickable action without callback being set.
        /// This method is only for subclasses of tickable action.
        /// If subclass is using this constructor, the method must be set at the
        /// subclass constructor.
        /// </summary>
        protected RsTickTask() { }

        /// <summary>
        /// Ticks this task.
        /// </summary>
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
        /// Resets this task.
        /// </summary>
        public virtual void Reset() => TickCount = 0;

        /// <summary>
        /// Cancels this task.
        /// </summary>
        public virtual void Cancel() => IsCancelled = true;

        private void ReleaseUnmanagedResources() => TickActionMethod = null;

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~RsTickTask() => ReleaseUnmanagedResources();
    }
}
