using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Queues asynchronous continuations for execution by the owning game loop.
    /// </summary>
    public sealed class GameLoopSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<PendingContinuation> _pending = new();

        /// <inheritdoc />
        public override void Post(SendOrPostCallback callback, object? state)
        {
            ArgumentNullException.ThrowIfNull(callback);
            _pending.Enqueue(new PendingContinuation(callback, state, null));
        }

        /// <summary>
        /// Runs all continuations that are pending when this method is called.
        /// </summary>
        public void RunPending()
        {
            var previousContext = Current;
            SetSynchronizationContext(this);

            try
            {
                while (_pending.TryDequeue(out var continuation))
                {
                    if (continuation.Context?.CancellationToken.IsCancellationRequested == true)
                    {
                        continue;
                    }

                    var previousContinuationContext = Current;
                    SetSynchronizationContext(continuation.Context ?? (SynchronizationContext)this);
                    try
                    {
                        if (continuation.Context?.CancellationToken.IsCancellationRequested == true)
                        {
                            continue;
                        }

                        continuation.Callback(continuation.State);
                    }
                    finally
                    {
                        SetSynchronizationContext(previousContinuationContext);
                    }
                }
            }
            finally
            {
                SetSynchronizationContext(previousContext);
            }
        }

        internal SynchronizationContext CreateTaskContext(CancellationToken cancellationToken) => new TaskSynchronizationContext(this, cancellationToken);

        private void Enqueue(SendOrPostCallback callback, object? state, TaskSynchronizationContext context) =>
            _pending.Enqueue(new PendingContinuation(callback, state, context));

        private readonly record struct PendingContinuation(
            SendOrPostCallback Callback,
            object? State,
            TaskSynchronizationContext? Context);

        private sealed class TaskSynchronizationContext(
            GameLoopSynchronizationContext owner,
            CancellationToken cancellationToken) : SynchronizationContext
        {
            public CancellationToken CancellationToken { get; } = cancellationToken;

            public override void Post(SendOrPostCallback callback, object? state)
            {
                ArgumentNullException.ThrowIfNull(callback);
                owner.Enqueue(callback, state, this);
            }
        }
    }
}
