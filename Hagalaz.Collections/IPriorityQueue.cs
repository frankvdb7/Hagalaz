using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Hagalaz.Collections
{
    /// <summary>
    /// Defines the contract for a priority queue, a collection of items that have a priority.
    /// Items with higher priority are served before items with lower priority.
    /// </summary>
    /// <typeparam name="TItem">The type of the items in the queue.</typeparam>
    /// <typeparam name="TPriority">The type used to represent the priority, which must be comparable.</typeparam>
    /// <remarks>
    /// For performance-critical scenarios, it is recommended to use a concrete implementation directly
    /// rather than this interface, as this may allow the JIT compiler to perform better optimizations.
    /// </remarks>
    [PublicAPI]
    public interface IPriorityQueue<TItem, in TPriority> : IEnumerable<TItem?>
        where TPriority : IComparable<TPriority>
    {
        /// <summary>
        /// Adds an item to the queue with a specified priority. Lower priority values are considered higher priority.
        /// How ties and duplicates are handled is determined by the specific implementation.
        /// </summary>
        /// <param name="node">The item to add to the queue.</param>
        /// <param name="priority">The priority of the item.</param>
        void Enqueue(TItem node, TPriority priority);

        /// <summary>
        /// Removes and returns the item with the highest priority (the head of the queue).
        /// Tie-breaking behavior is determined by the implementation (e.g., insertion order).
        /// </summary>
        /// <returns>The item with the highest priority that was removed from the queue.</returns>
        TItem? Dequeue();

        /// <summary>
        /// Removes all items from the queue.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the queue contains a specific item.
        /// </summary>
        /// <param name="node">The item to locate in the queue.</param>
        /// <returns><c>true</c> if the item is found in the queue; otherwise, <c>false</c>.</returns>
        bool Contains(TItem node);

        /// <summary>
        /// Removes a specific item from the queue. The item does not need to be at the head.
        /// </summary>
        /// <param name="node">The item to remove.</param>
        void Remove(TItem node);

        /// <summary>
        /// Updates the priority of an existing item in the queue.
        /// </summary>
        /// <param name="node">The item whose priority is to be updated.</param>
        /// <param name="priority">The new priority for the item.</param>
        void UpdatePriority(TItem node, TPriority priority);

        /// <summary>
        /// Gets the item with the highest priority from the queue without removing it.
        /// </summary>
        /// <returns>The item with the highest priority, or <c>null</c> if the queue is empty.</returns>
        TItem? First { get; }

        /// <summary>
        /// Gets the number of items contained in the queue.
        /// </summary>
        int Count { get; }
    }
}
