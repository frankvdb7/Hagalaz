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
        private readonly CancellationTokenSource _cancellation;
        private Func<CancellationToken, Task>? _operation;
        private Task? _task;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsAsyncTask"/> class.
        /// </summary>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <param name="cancellationToken">An optional externally owned cancellation token.</param>
        public RsAsyncTask(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            _operation = _ => operation();
            _cancellation = CreateCancellationSource(cancellationToken);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RsAsyncTask"/> class with cooperative cancellation.
        /// </summary>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <param name="cancellationToken">An optional externally owned cancellation token.</param>
        public RsAsyncTask(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
            _cancellation = CreateCancellationSource(cancellationToken);
        }

        private static CancellationTokenSource CreateCancellationSource(CancellationToken cancellationToken) =>
            cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : new CancellationTokenSource();

        /// <inheritdoc />
        public bool IsCancelled => _task?.IsCanceled ?? _cancellation.IsCancellationRequested;

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
                    if (_cancellation.IsCancellationRequested)
                    {
                        return;
                    }

                    _task = _operation!(_cancellation.Token);
                }

                if (!_task.IsCompleted)
                {
                    return;
                }

                if (_task.IsCanceled)
                {
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
            _cancellation.Cancel();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _operation = null;
            _cancellation.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
