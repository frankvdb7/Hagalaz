using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Adapts a regular asynchronous operation to the tick-based scheduler without blocking a tick.
    /// </summary>
    public sealed class RsAsyncTask : ITaskItem, IDisposable
    {
        private readonly CancellationTokenSource _cancellation = new();
        private Func<Task>? _taskExec;
        private Task? _task;
        private int _isCancelled;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsAsyncTask"/> class.
        /// </summary>
        /// <param name="taskExec">The asynchronous operation to execute.</param>
        public RsAsyncTask(Func<Task> taskExec) => _taskExec = taskExec ?? throw new ArgumentNullException(nameof(taskExec));

        /// <inheritdoc />
        public bool IsCancelled => Volatile.Read(ref _isCancelled) != 0;

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
                if (_task is null)
                {
                    if (SynchronizationContext.Current is GameLoopSynchronizationContext gameLoopContext)
                    {
                        var previousContext = SynchronizationContext.Current;
                        SynchronizationContext.SetSynchronizationContext(gameLoopContext.CreateTaskContext(_cancellation.Token));
                        try
                        {
                            _task = _taskExec!();
                        }
                        finally
                        {
                            SynchronizationContext.SetSynchronizationContext(previousContext);
                        }
                    }
                    else
                    {
                        _task = _taskExec!();
                    }
                }

                if (!_task.IsCompleted)
                {
                    return;
                }

                if (_task.IsCanceled)
                {
                    Volatile.Write(ref _isCancelled, 1);
                    return;
                }

                _task.GetAwaiter().GetResult();
                IsCompleted = true;
            }
            catch
            {
                IsFaulted = true;
                throw;
            }
        }

        /// <inheritdoc />
        public void Cancel()
        {
            Volatile.Write(ref _isCancelled, 1);
            _cancellation.Cancel();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _taskExec = null;
            _cancellation.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
