using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Hagalaz.Collections
{
    /// <summary>
    /// Represents an asynchronous queue mechanism using semaphore to manage concurrent access to queued items.
    /// </summary>
    /// <typeparam name="T">The type of items in the queue.</typeparam>
    [PublicAPI]
    public class SemaphoreAsyncQueue<T> : IAsyncQueue<T>
    {
        /// <summary>
        /// The work items
        /// </summary>
        private readonly ConcurrentQueue<T> _workItems = new();

        /// <summary>
        /// The signal
        /// </summary>
        private SemaphoreSlim _signal = new(0);

        /// <summary>
        /// Queues the background work item.
        /// </summary>
        /// <param name="item">The work item.</param>
        /// <exception cref="System.ArgumentNullException">workItem</exception>
        public void Enqueue(T item)
        {
            _workItems.Enqueue(item);
            _signal.Release();
        }

        /// <summary>
        /// Dequeue the asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<T?> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }

        #region IDisposable Support

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}