using System;

namespace Hagalaz.Game.Abstractions.Tasks
{
    public class RsTask<TResult> : RsTask, ITaskItem<TResult>, IDisposable
    {
        /// <summary>
        /// Contains action perform executeAction.
        /// </summary>
        private Func<TResult> _executeMethod;
        private Action<TResult> _resultHandler;

        /// <summary>
        /// Creates new task.
        /// </summary>
        /// <param name="executeFunc">Method which to execute when action is performed.</param>
        /// <param name="executeDelay">The execute delay in ticks.</param>
        /// <param name="resultHandler">The result handler for when the task executes and returns a result.</param>
        public RsTask(Func<TResult> executeFunc, int executeDelay, Action<TResult>? resultHandler = null)
        {
            _executeMethod = executeFunc;
            _resultHandler = resultHandler ?? NoopResultHandler;
            ExecuteDelay = executeDelay;
        }

        ~RsTask() => ReleaseUnmanagedResources();

        protected override void Execute()
        {
            _resultHandler.Invoke(_executeMethod.Invoke());
            Complete();
        }

        private static void NoopResultHandler(TResult _) { }

        public void RegisterResultHandler(Action<TResult> resultHandler)
        {
            var currentHandler = _resultHandler;
            _resultHandler = result =>
            {
                currentHandler.Invoke(result);
                resultHandler.Invoke(result);
            };
        }

        private void ReleaseUnmanagedResources()
        {
            _executeMethod = null!;
            _resultHandler = null!;
        }

        public new void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Class for standard actions.
    /// </summary>
    public class RsTask : ITaskItem, IDisposable
    {
        /// <summary>
        /// Contains action perform executeAction.
        /// </summary>
        protected Action ExecuteHandler;

        /// <summary>
        /// Contains execute delay in ticks.
        /// </summary>
        /// <value>The execute delay in ticks.</value>
        protected int ExecuteDelay { get; set; }

        /// <summary>
        /// Whether this task is cancelled.
        /// </summary>
        public bool IsCancelled { get; private set; }
        /// <summary>
        /// Whether this task is completed.
        /// </summary>
        public bool IsCompleted { get; private set; }
        /// <summary>
        /// Whether this task is faulted.
        /// </summary>
        public bool IsFaulted { get; private set; }

        /// <summary>
        /// Creates new task.
        /// </summary>
        /// <param name="executeHandler">Method which to execute when action is performed.</param>
        /// <param name="executeDelay">The execute delay in ticks.</param>
        public RsTask(Action executeHandler, int executeDelay)
        {
            ExecuteHandler = executeHandler;
            ExecuteDelay = executeDelay;
        }

        /// <summary>
        /// Creates new action without callback being set.
        /// This executeAction is only for subclasses of action.
        /// If subclass is using this constructor, the executeAction must be set at the
        /// subclass constructor.
        /// </summary>
        protected RsTask() => ExecuteHandler = NoopExecuteHandler;

        /// <summary>
        /// Ticks this task once.
        /// </summary>
        public void Tick()
        {
            if (IsCancelled || IsCompleted || IsFaulted)
            {
                return;
            }

            if (--ExecuteDelay > 0)
            {
                return;
            }

            try
            {
                Execute();
            }
            catch
            {
                IsFaulted = true;
                throw;
            }
        }

        /// <summary>
        /// Cancels this task.
        /// </summary>
        public void Cancel() => IsCancelled = true;

        protected virtual void Execute()
        {
            ExecuteHandler.Invoke();
            Complete();
        }

        protected void Complete() => IsCompleted = true;

        private void ReleaseUnmanagedResources() => ExecuteHandler = null!;

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~RsTask() => ReleaseUnmanagedResources();

        private static void NoopExecuteHandler() { }
    }
}