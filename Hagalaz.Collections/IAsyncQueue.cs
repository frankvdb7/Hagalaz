using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Hagalaz.Collections
{
    /// <summary>
    /// Defines the contract for a thread-safe, asynchronous queue that allows items to be
    /// enqueued and dequeued asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of elements in the asynchronous queue.</typeparam>
    [PublicAPI]
    public interface IAsyncQueue<T>
    {
        /// <summary>
        /// Adds an item to the end of the queue.
        /// </summary>
        /// <param name="item">The item to add to the queue.</param>
        void Enqueue(T item);

        /// <summary>
        /// Asynchronously removes and returns the item at the beginning of the queue.
        /// If the queue is empty, this method waits until an item becomes available.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous dequeue operation. The value of the task's result is the item
        /// that is removed from the beginning of the queue. Returns <c>null</c> if the operation is cancelled.
        /// </returns>
        Task<T?> DequeueAsync(CancellationToken cancellationToken);
    }
}