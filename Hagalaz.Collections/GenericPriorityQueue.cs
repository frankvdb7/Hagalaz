using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Hagalaz.Collections
{
    /// <summary>
    /// Represents a node for use in a <see cref="GenericPriorityQueue{TItem, TPriority}"/>.
    /// It encapsulates the priority, queue position, and insertion order for an item.
    /// </summary>
    /// <typeparam name="TPriority">The type used for priority, which must be comparable.</typeparam>
    public class GenericPriorityQueueNode<TPriority>
        where TPriority : IComparable<TPriority>
    {
        /// <summary>
        /// Gets or sets the priority of this node. This value should be set before enqueueing
        /// and only modified via the <see cref="GenericPriorityQueue{TItem, TPriority}.UpdatePriority"/> method thereafter.
        /// </summary>
        public TPriority Priority { get; protected internal set; } = default!;

        /// <summary>
        /// Gets the internal index of this node within the queue's heap structure.
        /// This property is managed by the queue and should not be modified manually.
        /// </summary>
        public int QueueIndex { get; internal set; }

        /// <summary>
        /// Gets the unique, sequential index assigned to this node upon insertion, used for maintaining stable sorting.
        /// This property is managed by the queue and should not be modified manually.
        /// </summary>
        public long InsertionIndex { get; internal set; }
    }

    /// <summary>
    /// A high-performance, stable, min-priority queue with a generic priority type. It has a fixed maximum size
    /// and provides O(1) time complexity for the <c>Contains</c> operation. Ties in priority are broken
    /// by the order of insertion (first-in, first-out).
    /// </summary>
    /// <typeparam name="TItem">The type of items in the queue, which must inherit from <see cref="GenericPriorityQueueNode{TPriority}"/>.</typeparam>
    /// <typeparam name="TPriority">The type used for priority, which must implement <see cref="IComparable{T}"/>.</typeparam>
    [PublicAPI]
    public sealed class GenericPriorityQueue<TItem, TPriority> : IFixedSizePriorityQueue<TItem, TPriority>
        where TItem : GenericPriorityQueueNode<TPriority>
        where TPriority : IComparable<TPriority>
    {
        private TItem?[] _nodes;
        private long _numNodesEverEnqueued;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericPriorityQueue{TItem, TPriority}"/> class
        /// with a specified maximum capacity.
        /// </summary>
        /// <param name="maxNodes">The maximum number of nodes that the queue can hold.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="maxNodes"/> is less than or equal to 0.</exception>
        public GenericPriorityQueue(int maxNodes)
        {
#if DEBUG
            if (maxNodes <= 0)
            {
                throw new InvalidOperationException("New queue size cannot be smaller than 1");
            }
#endif

            Count = 0;
            _nodes = new TItem[maxNodes + 1];
            _numNodesEverEnqueued = 0;
        }

        /// <summary>
        /// Gets the number of nodes currently in the queue.
        /// Time complexity: O(1).
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the maximum number of items that can be enqueued in this queue.
        /// Time complexity: O(1).
        /// </summary>
        public int MaxSize => _nodes.Length - 1;

        /// <summary>
        /// Removes all nodes from the queue.
        /// Time complexity: O(n), where n is the current count of items in the queue.
        /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Clear()
        {
            Array.Clear(_nodes, 1, Count);
            Count = 0;
        }

        /// <summary>
        /// Determines whether the specified node is present in the queue.
        /// Time complexity: O(1).
        /// </summary>
        /// <param name="node">The node to locate in the queue.</param>
        /// <returns><c>true</c> if the node is found in the queue; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="node"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown in DEBUG builds if the node's queue index is corrupted.</exception>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool Contains(TItem node)
        {
#if DEBUG
            ArgumentNullException.ThrowIfNull(node);

            if (node.QueueIndex < 0 || node.QueueIndex >= _nodes.Length)
            {
                throw new InvalidOperationException("node.QueueIndex has been corrupted. Did you change it manually? Or add this node to another queue?");
            }
#endif

            return (_nodes[node.QueueIndex] == node);
        }

        /// <summary>
        /// Adds a node to the priority queue. Lower priority values are placed first. Ties are broken by insertion order.
        /// Time complexity: O(log n).
        /// </summary>
        /// <param name="node">The node to enqueue.</param>
        /// <param name="priority">The priority of the node.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="node"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown in DEBUG builds if the queue is full or if the node is already enqueued.
        /// In RELEASE builds, such actions result in undefined behavior.
        /// </exception>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void Enqueue(TItem node, TPriority priority)
        {
#if DEBUG
            ArgumentNullException.ThrowIfNull(node);

            if (Count >= _nodes.Length - 1)
            {
                throw new InvalidOperationException("Queue is full - node cannot be added: " + node);
            }

            if (Contains(node))
            {
                throw new InvalidOperationException("Node is already enqueued: " + node);
            }
#endif

            node.Priority = priority;
            Count++;
            _nodes[Count] = node;
            node.QueueIndex = Count;
            node.InsertionIndex = _numNodesEverEnqueued++;
            CascadeUp(node);
        }

#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void Swap(TItem node1, TItem node2)
        {
            //Swap the nodes
            _nodes[node1.QueueIndex] = node2;
            _nodes[node2.QueueIndex] = node1;

            //Swap their indices
            (node1.QueueIndex, node2.QueueIndex) = (node2.QueueIndex, node1.QueueIndex);
        }

        //Performance appears to be slightly better when this is NOT inlined o_O
        private void CascadeUp(TItem node)
        {
            //aka Heapify-up
            var parent = node.QueueIndex / 2;
            while (parent >= 1)
            {
                var parentNode = _nodes[parent];
                if (parentNode != null)
                {
                    if (HasHigherPriority(parentNode, node))
                    {
                        break;
                    }

                    //Node has lower priority value, so move it up the heap
                    Swap(node, parentNode); //For some reason, this is faster with Swap() rather than (less..?) individual operations, like in CascadeDown()
                }

                parent = node.QueueIndex / 2;
            }
        }

#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void CascadeDown(TItem node)
        {
            //aka Heapify-down
            var finalQueueIndex = node.QueueIndex;
            while (true)
            {
                var newParent = node;
                var childLeftIndex = 2 * finalQueueIndex;

                //Check if the left-child is higher-priority than the current node
                if (childLeftIndex > Count)
                {
                    //This could be placed outside the loop, but then we'd have to check newParent != node twice
                    node.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = node;
                    break;
                }

                var childLeft = _nodes[childLeftIndex];
                if (childLeft != null && HasHigherPriority(childLeft, newParent))
                {
                    newParent = childLeft;
                }

                //Check if the right-child is higher-priority than either the current node or the left child
                var childRightIndex = childLeftIndex + 1;
                if (childRightIndex <= Count)
                {
                    var childRight = _nodes[childRightIndex];
                    if (childRight != null && HasHigherPriority(childRight, newParent))
                    {
                        newParent = childRight;
                    }
                }

                //If either of the children has higher (smaller) priority, swap and continue cascading
                if (newParent != node)
                {
                    //Move new parent to its new index.  node will be moved once, at the end
                    //Doing it this way is one less assignment operation than calling Swap()
                    _nodes[finalQueueIndex] = newParent;

                    (newParent.QueueIndex, finalQueueIndex) = (finalQueueIndex, newParent.QueueIndex);
                }
                else
                {
                    //See note above
                    node.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = node;
                    break;
                }
            }
        }

        /// <summary>
        /// Returns true if 'higher' has higher priority than 'lower', false otherwise.
        /// Note that calling HasHigherPriority(node, node) (ie. both arguments the same node) will return false
        /// </summary>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static bool HasHigherPriority(TItem higher, TItem lower)
        {
            var cmp = higher.Priority.CompareTo(lower.Priority);
            return (cmp < 0 || (cmp == 0 && higher.InsertionIndex < lower.InsertionIndex));
        }

        /// <summary>
        /// Removes the node with the highest priority (the head of the queue) and returns it.
        /// Ties are broken by insertion order. Time complexity: O(log n).
        /// </summary>
        /// <returns>The node with the highest priority in the queue.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown in DEBUG builds if the queue is empty or corrupted.
        /// In RELEASE builds, dequeueing from an empty queue results in undefined behavior.
        /// </exception>
        public TItem? Dequeue()
        {
#if DEBUG
            if (Count <= 0)
            {
                throw new InvalidOperationException("Cannot call Dequeue() on an empty queue");
            }

            if (!IsValidQueue())
            {
                throw new InvalidOperationException("Queue has been corrupted (Did you update a node priority manually instead of calling UpdatePriority()?" +
                                                    "Or add the same node to two different queues?)");
            }
#endif

            var returnMe = _nodes[1];
            if (returnMe != null)
            {
                Remove(returnMe);
            }

            return returnMe;
        }

        /// <summary>
        /// Resizes the queue's internal array to accommodate a new maximum number of nodes.
        /// All currently enqueued nodes are preserved. Time complexity: O(n).
        /// </summary>
        /// <param name="maxNodes">The new maximum capacity of the queue.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown in DEBUG builds if <paramref name="maxNodes"/> is not a positive number
        /// or if it is smaller than the current number of nodes in the queue.
        /// </exception>
        public void Resize(int maxNodes)
        {
#if DEBUG
            if (maxNodes <= 0)
            {
                throw new InvalidOperationException("Queue size cannot be smaller than 1");
            }

            if (maxNodes < Count)
            {
                throw new InvalidOperationException("Called Resize(" + maxNodes + "), but current queue contains " + Count + " nodes");
            }
#endif

            var newArray = new TItem?[maxNodes + 1];
            var highestIndexToCopy = Math.Min(maxNodes, Count);
            for (var i = 1; i <= highestIndexToCopy; i++)
            {
                newArray[i] = _nodes[i];
            }

            _nodes = newArray;
        }

        /// <summary>
        /// Gets the node with the highest priority (the head of the queue) without removing it.
        /// Time complexity: O(1).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown in DEBUG builds if the queue is empty.
        /// In RELEASE builds, accessing First on an empty queue results in undefined behavior.
        /// </exception>
        public TItem? First
        {
            get
            {
#if DEBUG
                if (Count <= 0)
                {
                    throw new InvalidOperationException("Cannot call .First on an empty queue");
                }
#endif

                return _nodes[1];
            }
        }

        /// <summary>
        /// This method must be called on a node every time its priority changes while it is in the queue.
        /// Forgetting to call this method will result in a corrupted queue. Time complexity: O(log n).
        /// </summary>
        /// <param name="node">The node in the queue that has a new priority.</param>
        /// <param name="priority">The new priority of the node.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="node"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown in DEBUG builds if the node is not in the queue.
        /// In RELEASE builds, such an action results in undefined behavior.
        /// </exception>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public void UpdatePriority(TItem node, TPriority priority)
        {
#if DEBUG
            ArgumentNullException.ThrowIfNull(node);

            if (!Contains(node))
            {
                throw new InvalidOperationException("Cannot call UpdatePriority() on a node which is not enqueued: " + node);
            }
#endif

            node.Priority = priority;
            OnNodeUpdated(node);
        }

        private void OnNodeUpdated(TItem node)
        {
            //Bubble the updated node up or down as appropriate
            var parentIndex = node.QueueIndex / 2;
            var parentNode = _nodes[parentIndex];

            if (parentIndex > 0 && parentNode != null && HasHigherPriority(node, parentNode))
            {
                CascadeUp(node);
            }
            else
            {
                //Note that CascadeDown will be called if parentNode == node (that is, node is the root)
                CascadeDown(node);
            }
        }

        /// <summary>
        /// Removes a specific node from the queue. The node does not need to be the head.
        /// If the node is not in the queue, behavior is undefined. Use <see cref="Contains(TItem)"/> first if unsure.
        /// Time complexity: O(log n).
        /// </summary>
        /// <param name="node">The node to remove from the queue.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="node"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown in DEBUG builds if the node is not in the queue.</exception>
        public void Remove(TItem node)
        {
#if DEBUG
            ArgumentNullException.ThrowIfNull(node);

            if (!Contains(node))
            {
                throw new InvalidOperationException("Cannot call Remove() on a node which is not enqueued: " + node);
            }
#endif

            //If the node is already the last node, we can remove it immediately
            if (node.QueueIndex == Count)
            {
                _nodes[Count] = null;
                Count--;
                return;
            }

            //Swap the node with the last node
            var formerLastNode = _nodes[Count];
            if (formerLastNode != null)
            {
                Swap(node, formerLastNode);
            }

            _nodes[Count] = null;
            Count--;

            if (formerLastNode != null)
            {
                //Now bubble formerLastNode (which is no longer the last node) up or down as appropriate
                OnNodeUpdated(formerLastNode);
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.</returns>
        public IEnumerator<TItem?> GetEnumerator()
        {
            for (var i = 1; i <= Count; i++) yield return _nodes[i];
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
            for (var i = 1; i < _nodes.Length; i++)
            {
                if (_nodes[i] == null)
                {
                    continue;
                }

                var childLeftIndex = 2 * i;
                if (childLeftIndex < _nodes.Length && _nodes[childLeftIndex] != null && _nodes[i] != null &&
                    HasHigherPriority(_nodes[childLeftIndex]!, _nodes[i]!))
                {
                    return false;
                }

                var childRightIndex = childLeftIndex + 1;
                if (childRightIndex < _nodes.Length && _nodes[childRightIndex] != null && _nodes[i] != null &&
                    HasHigherPriority(_nodes[childRightIndex]!, _nodes[i]!))
                {
                    return false;
                }
            }

            return true;
        }
    }
}