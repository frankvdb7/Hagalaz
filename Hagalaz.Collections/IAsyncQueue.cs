using System.Threading;
using System.Threading.Tasks;

namespace Hagalaz.Collections
{
    /// <summary>
    /// 
    /// </summary>
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
        Task<T> DequeueAsync(CancellationToken cancellationToken);
    }
}