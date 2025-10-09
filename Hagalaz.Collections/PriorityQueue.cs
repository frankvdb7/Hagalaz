using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Hagalaz.Collections
{
    /// <summary>
    /// A user-friendly, thread-safe, and stable priority queue that automatically resizes.
    /// It is a wrapper around <see cref="GenericPriorityQueue{TItem, TPriority}"/>, providing a simpler API
    /// at the cost of some performance overhead for operations like Contains, Remove, and UpdatePriority.
    /// </summary>
    /// <typeparam name="TItem">The type of items to enqueue.</typeparam>
    /// <typeparam name="TPriority">The type used for priority, which must implement <see cref="IComparable{T}"/>.</typeparam>
    [PublicAPI]
    public class PriorityQueue<TItem, TPriority> : IPriorityQueue<TItem, TPriority>
        where TPriority : IComparable<TPriority>
    {
        private class SimpleNode : GenericPriorityQueueNode<TPriority>
        {
            public TItem Data { get; private set; }

            public SimpleNode(TItem data) => Data = data;
        }

        private const int _initialQueueSize = 10;
        private readonly GenericPriorityQueue<SimpleNode, TPriority> _queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityQueue{TItem, TPriority}"/> class.
        /// </summary>
        public PriorityQueue() => _queue = new GenericPriorityQueue<SimpleNode, TPriority>(_initialQueueSize);

        /// <summary>
        /// Finds and returns the internal node wrapper for a given item.
        /// </summary>
        private SimpleNode GetExistingNode(TItem item)
        {
            var comparer = EqualityComparer<TItem>.Default;
            foreach (var node in _queue)
            {
                if (node != null && comparer.Equals(node.Data, item))
                {
                    return node;
                }
            }
            throw new InvalidOperationException("Item cannot be found in queue: " + item);
        }

        /// <summary>
        /// Gets the number of items currently in the queue.
        /// Time complexity: O(1).
        /// </summary>
        public int Count
        {
            get
            {
                lock (_queue)
                {
                    return _queue.Count;
                }
            }
        }


        /// <summary>
        /// Gets the item with the highest priority from the queue without removing it.
        /// Time complexity: O(1).
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the queue is empty.</exception>
        public TItem? First
        {
            get
            {
                lock (_queue)
                {
                    if (_queue.Count <= 0)
                    {
                        throw new InvalidOperationException("Cannot call .First on an empty queue");
                    }

                    var first = _queue.First;
                    return (first != null ? first.Data : default);
                }
            }
        }

        /// <summary>
        /// Removes all items from the queue.
        /// Time complexity: O(n).
        /// </summary>
        public void Clear()
        {
            lock (_queue)
            {
                _queue.Clear();
            }
        }

        /// <summary>
        /// Determines whether the queue contains a specific item.
        /// Time complexity: O(n).
        /// </summary>
        /// <param name="item">The item to locate in the queue.</param>
        /// <returns><c>true</c> if the item is found; otherwise, <c>false</c>.</returns>
        public bool Contains(TItem item)
        {
            lock (_queue)
            {
                var comparer = EqualityComparer<TItem>.Default;
                return _queue.Where(node => node != null).Any(node => comparer.Equals(node!.Data, item));
            }
        }

        /// <summary>
        /// Removes and returns the item with the highest priority from the head of the queue.
        /// Ties are broken by insertion order.
        /// Time complexity: O(log n).
        /// </summary>
        /// <returns>The item that was removed from the queue.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the queue is empty.</exception>
        public TItem? Dequeue()
        {
            lock (_queue)
            {
                if (_queue.Count <= 0)
                {
                    throw new InvalidOperationException("Cannot call Dequeue() on an empty queue");
                }

                var node = _queue.Dequeue();
                return node == null ? default : node.Data;
            }
        }

        /// <summary>
        /// Adds an item to the queue with a specified priority.
        /// Lower priority values are considered higher priority. The queue automatically resizes if necessary.
        /// Duplicates are permitted.
        /// Time complexity: O(log n).
        /// </summary>
        /// <param name="item">The item to add to the queue.</param>
        /// <param name="priority">The priority of the item.</param>
        public void Enqueue(TItem item, TPriority priority)
        {
            lock (_queue)
            {
                var node = new SimpleNode(item);
                if (_queue.Count == _queue.MaxSize)
                {
                    _queue.Resize(_queue.MaxSize * 2 + 1);
                }
                _queue.Enqueue(node, priority);
            }
        }

        /// <summary>
        /// Removes an item from the queue. This item does not need to be at the head.
        /// If multiple instances of the same item exist, only the first one found is removed.
        /// Time complexity: O(n).
        /// </summary>
        /// <param name="item">The item to remove.</param>
        /// <exception cref="InvalidOperationException">Thrown if the item is not found in the queue. Use <see cref="Contains(TItem)"/> to check for existence first.</exception>
        public void Remove(TItem item)
        {
            lock (_queue)
            {
                try
                {
                    _queue.Remove(GetExistingNode(item));
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException("Cannot call Remove() on a node which is not enqueued: " + item, ex);
                }
            }
        }

        /// <summary>
        /// Updates the priority of an item already in the queue.
        /// If the item is present multiple times, only the first one found is updated.
        /// Time complexity: O(n).
        /// </summary>
        /// <param name="item">The item whose priority is to be updated.</param>
        /// <param name="priority">The new priority for the item.</param>
        /// <exception cref="InvalidOperationException">Thrown if the item is not found in the queue.</exception>
        public void UpdatePriority(TItem item, TPriority priority)
        {
            lock (_queue)
            {
                try
                {
                    var updateMe = GetExistingNode(item);
                    _queue.UpdatePriority(updateMe, priority);
                }
                catch (InvalidOperationException ex)
                {
                    throw new InvalidOperationException("Cannot call UpdatePriority() on a node which is not enqueued: " + item, ex);
                }
            }
        }

        /// <summary>
        /// Returns a thread-safe enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.</returns>
        /// <remarks>This method creates a snapshot of the queue, so it is safe to use even if the queue is modified during enumeration.</remarks>
        public IEnumerator<TItem> GetEnumerator()
        {
            var queueData = new List<TItem>();
            lock (_queue)
            {
                //Copy to a separate list because we don't want to 'yield return' inside a lock
                queueData.AddRange(_queue.Where(node => node != null).Select(node => node!.Data));
            }

            return queueData.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Checks if the queue is still in a valid heap state.
        /// This method should not be called in production code as it is intended for testing and debugging purposes only.
        /// </summary>
        /// <returns><c>true</c> if the queue is a valid heap; otherwise, <c>false</c>.</returns>
        public bool IsValidQueue()
        {
            lock (_queue)
            {
                return _queue.IsValidQueue();
            }
        }
    }

    /// <summary>
    /// A convenient, user-friendly priority queue where the priority is a <see cref="float"/>.
    /// This class is stable, thread-safe, and automatically resizes. It inherits from <see cref="PriorityQueue{TItem, TPriority}"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of items to enqueue.</typeparam>
    [PublicAPI]
    public class SimplePriorityQueue<TItem> : PriorityQueue<TItem, float> { }
}
