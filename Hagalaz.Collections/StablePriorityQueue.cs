using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Hagalaz.Collections
{
    /// <summary>
    /// Represents a node for use in a <see cref="StablePriorityQueue{T}"/>.
    /// It extends <see cref="FixedPriorityQueueNode"/> with an <see cref="InsertionIndex"/>
    /// to ensure that items with the same priority are dequeued in the order they were enqueued (FIFO).
    /// </summary>
    public class StablePriorityQueueNode : FixedPriorityQueueNode
    {
        /// <summary>
        /// Gets the unique, sequential index assigned to this node upon insertion.
        /// This property is managed by the queue and should not be modified manually.
        /// </summary>
        public long InsertionIndex { get; internal set; }
    }

    /// <summary>
    /// A high-performance, stable, min-priority queue implementation with a fixed capacity that can be resized.
    /// Stability ensures that when two nodes are enqueued with the same priority, they are dequeued in the same
    /// order they were inserted. It provides O(1) time complexity for the <c>Contains</c> operation.
    /// </summary>
    /// <typeparam name="T">The type of items in the queue, which must inherit from <see cref="StablePriorityQueueNode"/>.</typeparam>
    /// <remarks>
    /// Based on the implementation by BlueRaja:
    /// See https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp/wiki/Getting-Started for more information.
    /// </remarks>
    [PublicAPI]
    public sealed class StablePriorityQueue<T> : IFixedSizePriorityQueue<T, float>
        where T : StablePriorityQueueNode
    {
        private T?[] _nodes;
        private long _numNodesEverEnqueued;

        /// <summary>
        /// Initializes a new instance of the <see cref="StablePriorityQueue{T}"/> class
        /// with a specified maximum capacity.
        /// </summary>
        /// <param name="maxNodes">The maximum number of nodes that the queue can hold.</param>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="maxNodes"/> is less than or equal to 0.</exception>
        public StablePriorityQueue(int maxNodes)
        {
#if DEBUG
            if (maxNodes <= 0)
            {
                throw new InvalidOperationException("New queue size cannot be smaller than 1");
            }
#endif

            Count = 0;
            _nodes = new T[maxNodes + 1];
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
        /// Time complexity: O(n).
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
        /// <returns><c>true</c> if the node is found; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="node"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown in DEBUG builds if the node's queue index is corrupted.</exception>
#if NET_VERSION_4_5
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public bool Contains(T node)
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
        public void Enqueue(T node, float priority)
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
        private void Swap(T node1, T node2)
        {
            //Swap the nodes
            _nodes[node1.QueueIndex] = node2;
            _nodes[node2.QueueIndex] = node1;

            //Swap their indicies
            (node1.QueueIndex, node2.QueueIndex) = (node2.QueueIndex, node1.QueueIndex);
        }

        //Performance appears to be slightly better when this is NOT inlined o_O
        private void CascadeUp(T node)
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
        private void CascadeDown(T node)
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
        private static bool HasHigherPriority(T higher, T lower)
        {
            return (higher.Priority < lower.Priority ||
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    (higher.Priority == lower.Priority && higher.InsertionIndex < lower.InsertionIndex));
        }

        /// <summary>
        /// Removes and returns the node with the highest priority (the head of the queue).
        /// Ties are broken by insertion order. Time complexity: O(log n).
        /// </summary>
        /// <returns>The node with the highest priority.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown in DEBUG builds if the queue is empty or corrupted.
        /// In RELEASE builds, dequeueing from an empty queue results in undefined behavior.
        /// </exception>
        public T? Dequeue()
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
        /// Resizes the queue's internal array to a new maximum capacity.
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

            var newArray = new T?[maxNodes + 1];
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
        public T? First
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
        public void UpdatePriority(T node, float priority)
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

        private void OnNodeUpdated(T node)
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
        /// Removes a specific node from the queue. The node does not need to be at the head.
        /// If the node is not in the queue, behavior is undefined. Use <see cref="Contains(T)"/> first if unsure.
        /// Time complexity: O(log n).
        /// </summary>
        /// <param name="node">The node to remove from the queue.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="node"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown in DEBUG builds if the node is not in the queue.</exception>
        public void Remove(T node)
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
        public IEnumerator<T?> GetEnumerator()
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