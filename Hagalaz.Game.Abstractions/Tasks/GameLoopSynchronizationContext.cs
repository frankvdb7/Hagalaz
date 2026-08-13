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
        private ConcurrentQueue<PendingContinuation> _pending = new();

        /// <inheritdoc />
        public override void Post(SendOrPostCallback callback, object? state)
        {
            ArgumentNullException.ThrowIfNull(callback);
            _pending.Enqueue(new PendingContinuation(callback, state));
        }

        /// <summary>
        /// Runs the continuations that are pending when this method is called.
        /// </summary>
        public void RunPending()
        {
            var pending = Interlocked.Exchange(ref _pending, new ConcurrentQueue<PendingContinuation>());
            var previousContext = Current;
            SetSynchronizationContext(this);

            try
            {
                while (pending.TryDequeue(out var continuation))
                {
                    continuation.Callback(continuation.State);
                }
            }
            finally
            {
                SetSynchronizationContext(previousContext);
            }
        }

        private readonly record struct PendingContinuation(
            SendOrPostCallback Callback,
            object? State);
    }
}
