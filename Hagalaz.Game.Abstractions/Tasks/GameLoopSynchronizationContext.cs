using System;
using System.Collections.Generic;
using System.Threading;

namespace Hagalaz.Game.Abstractions.Tasks
{
    /// <summary>
    /// Queues asynchronous continuations for execution by the owning game loop.
    /// </summary>
    public sealed class GameLoopSynchronizationContext : SynchronizationContext
    {
        private readonly Lock _sync = new();
        private Queue<PendingContinuation> _pending = new();

        /// <inheritdoc />
        public override void Post(SendOrPostCallback callback, object? state)
        {
            ArgumentNullException.ThrowIfNull(callback);

            lock (_sync)
            {
                _pending.Enqueue(new PendingContinuation(callback, state));
            }
        }

        /// <summary>
        /// Runs the continuations that are pending when this method is called.
        /// </summary>
        public void RunPending()
        {
            Queue<PendingContinuation> pending;
            lock (_sync)
            {
                pending = _pending;
                _pending = new Queue<PendingContinuation>();
            }

            var previousContext = Current;
            SetSynchronizationContext(this);

            try
            {
                while (pending.Count > 0)
                {
                    var continuation = pending.Dequeue();
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
