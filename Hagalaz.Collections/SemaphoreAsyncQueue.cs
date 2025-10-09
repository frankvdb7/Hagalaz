using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Hagalaz.Collections
{
    /// <summary>
    /// A thread-safe implementation of an asynchronous queue based on <see cref="SemaphoreSlim"/>.
    /// This queue is suitable for producer-consumer scenarios where consumers need to wait asynchronously
    /// for items to become available.
    /// </summary>
    /// <typeparam name="T">The type of items stored in the queue.</typeparam>
    [PublicAPI]
    public class SemaphoreAsyncQueue<T> : IAsyncQueue<T>
    {
        /// <summary>
        /// The underlying thread-safe queue for storing items.
        /// </summary>
        private readonly ConcurrentQueue<T> _workItems = new();

        /// <summary>
        /// The semaphore used to signal the availability of items in the queue.
        /// </summary>
        private SemaphoreSlim _signal = new(0);

        /// <summary>
        /// Adds an item to the end of the queue and signals waiting consumers that a new item is available.
        /// </summary>
        /// <param name="item">The item to add to the queue.</param>
        public void Enqueue(T item)
        {
            _workItems.Enqueue(item);
            _signal.Release();
        }

        /// <summary>
        /// Asynchronously removes and returns the item at the beginning of the queue.
        /// If the queue is empty, this method waits until an item is available or the operation is cancelled.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for an item.</param>
        /// <returns>
        /// A task that represents the asynchronous dequeue operation. The result of the task is the item from the
        /// beginning of the queue, or <c>default(T)</c> if the operation fails after the signal is received.
        /// </returns>
        public async Task<T?> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }

        #region IDisposable Support

        /// <summary>
        /// Releases the managed resources used by the <see cref="SemaphoreAsyncQueue{T}"/>.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release managed resources; otherwise, <c>false</c>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _signal == null!)
            {
                return;
            }
            _signal.Dispose();
            _signal = null!;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}