using System;
using JetBrains.Annotations;

namespace Hagalaz.Collections
{
    /// <summary>
    /// Extends the <see cref="IPriorityQueue{TItem, TPriority}"/> interface to include functionality
    /// specific to priority queues with a fixed, but resizable, capacity.
    /// </summary>
    /// <remarks>
    /// This interface is marked as internal and is primarily intended to facilitate unit testing.
    /// </remarks>
    [PublicAPI]
    internal interface IFixedSizePriorityQueue<TItem, in TPriority> : IPriorityQueue<TItem, TPriority>
        where TPriority : IComparable<TPriority>
    {
        /// <summary>
        /// Resizes the queue's internal storage to a new specified maximum capacity.
        /// All currently enqueued nodes are preserved. Attempting to resize to a capacity smaller
        /// than the current number of items will result in undefined behavior.
        /// </summary>
        /// <param name="maxNodes">The new maximum number of nodes the queue can hold.</param>
        void Resize(int maxNodes);

        /// <summary>
        /// Gets the maximum number of items that can be enqueued in this queue at one time.
        /// Exceeding this limit will result in undefined behavior.
        /// </summary>
        int MaxSize { get; }
    }
}
