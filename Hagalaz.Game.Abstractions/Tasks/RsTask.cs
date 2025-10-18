using System;

namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Represents a delayed-execution task that returns a result of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
    public class RsTask<TResult> : RsTask, ITaskItem<TResult>, IDisposable
    {
        private Func<TResult> _executeMethod;
        private Action<TResult> _resultHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsTask{TResult}"/> class.
        /// </summary>
        /// <param name="executeFunc">The function to execute when the task runs.</param>
        /// <param name="executeDelay">The delay in game ticks before the task executes.</param>
        /// <param name="resultHandler">An optional handler to process the result of the task.</param>
        public RsTask(Func<TResult> executeFunc, int executeDelay, Action<TResult>? resultHandler = null)
        {
            _executeMethod = executeFunc;
            _resultHandler = resultHandler ?? NoopResultHandler;
            ExecuteDelay = executeDelay;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="RsTask{TResult}"/> class.
        /// </summary>
        ~RsTask() => ReleaseUnmanagedResources();

        /// <inheritdoc />
        protected override void Execute()
        {
            _resultHandler.Invoke(_executeMethod.Invoke());
            Complete();
        }

        private static void NoopResultHandler(TResult _) { }

        /// <inheritdoc />
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

        /// <summary>
        /// Releases the resources used by the task.
        /// </summary>
        public new void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Represents a standard, delayed-execution task that does not return a result.
    /// </summary>
    public class RsTask : ITaskItem, IDisposable
    {
        /// <summary>
        /// The action to perform when the task executes.
        /// </summary>
        protected Action ExecuteHandler;

        /// <summary>
        /// The remaining delay in ticks before the task executes.
        /// </summary>
        protected int ExecuteDelay { get; set; }

        /// <inheritdoc />
        public bool IsCancelled { get; private set; }

        /// <inheritdoc />
        public bool IsCompleted { get; private set; }

        /// <inheritdoc />
        public bool IsFaulted { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RsTask"/> class.
        /// </summary>
        /// <param name="executeHandler">The delegate to execute when the task runs.</param>
        /// <param name="executeDelay">The delay in game ticks before execution.</param>
        public RsTask(Action executeHandler, int executeDelay)
        {
            ExecuteHandler = executeHandler;
            ExecuteDelay = executeDelay;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RsTask"/> class without a specific callback.
        /// This is intended for use by subclasses that will set their own execution logic.
        /// </summary>
        protected RsTask() => ExecuteHandler = NoopExecuteHandler;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void Cancel() => IsCancelled = true;

        /// <summary>
        /// Executes the task's logic. Can be overridden by subclasses for custom behavior.
        /// </summary>
        protected virtual void Execute()
        {
            ExecuteHandler.Invoke();
            Complete();
        }

        /// <summary>
        /// Marks the task as completed.
        /// </summary>
        protected void Complete() => IsCompleted = true;

        private void ReleaseUnmanagedResources() => ExecuteHandler = null!;

        /// <summary>
        /// Releases the resources used by the task.
        /// </summary>
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="RsTask"/> class.
        /// </summary>
        ~RsTask() => ReleaseUnmanagedResources();

        private static void NoopExecuteHandler() { }
    }
}