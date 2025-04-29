using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Hagalaz.Collections
{
    /// <summary>
    /// Represents an asynchronous queue that allows enqueuing items and dequeuing them asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of items in the queue.</typeparam>
    [PublicAPI]
    public interface IAsyncQueue<T>
    {
        /// <summary>
        /// Queues the item.
        /// </summary>
        /// <param name="item">The item.</param>
        void Enqueue(T item);

        /// <summary>
        /// Dequeue the asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<T?> DequeueAsync(CancellationToken cancellationToken);
    }
}