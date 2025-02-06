using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hagalaz.Workers
{
    public interface IBackgroundTaskQueue
    {
        /// <summary>
        /// Queues the background work item.
        /// </summary>
        /// <param name="workItem">The work item.</param>
        ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);

        /// <summary>
        /// Dequeues the asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
    }
}
