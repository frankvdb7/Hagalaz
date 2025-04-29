using System.Collections;
using System.Collections.Generic;
using System.Threading;
#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement

namespace Hagalaz.Collections
{
    /// <summary>
    /// Represents a thread-safe, synchronized wrapper around a generic list that ensures thread safety for all operations.
    /// </summary>
    /// <typeparam name="T">The type of elements contained in the list.</typeparam>
    /// <remarks>
    /// The <c>SynchronizedList</c> provides a collection that can be safely accessed by multiple threads.
    /// All operations performed on the list, such as adding, removing, or retrieving items, are synchronized using an internal lock object.
    /// This class is useful for scenarios where shared access to a list across multiple threads is required while maintaining thread safety.
    /// </remarks>
    public class SynchronizedList<T> : IList<T>
    {
        private readonly List<T> _list;

        /// <summary>
        /// 
        /// </summary>
        public SynchronizedList() => _list = [];

        public Lock SyncRoot { get; } = new();

        public int Count
        {
            get
            {
                lock (SyncRoot)
                {
                    return _list.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            lock (SyncRoot)
            {
                _list.Add(item);
            }
        }

        public void Clear()
        {
            lock (SyncRoot)
            {
                _list.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (SyncRoot)
            {
                return _list.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                _list.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            lock (SyncRoot)
            {
                return _list.Remove(item);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (SyncRoot)
            {
                return new SynchronizedEnumerator<T>(_list.GetEnumerator(), SyncRoot);
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            lock (SyncRoot)
            {
                return new SynchronizedEnumerator<T>(_list.GetEnumerator(), SyncRoot);
            }
        }

        public T this[int index]
        {
            get
            {
                lock (SyncRoot)
                {
                    return _list[index];
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    _list[index] = value;
                }
            }
        }

        public int IndexOf(T item)
        {
            lock (SyncRoot)
            {
                return _list.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (SyncRoot)
            {
                _list.Insert(index, item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (SyncRoot)
            {
                _list.RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// Provides a thread-safe enumerator that ensures synchronized access to the underlying collection during enumeration.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection being enumerated.</typeparam>
    /// <remarks>
    /// The <c>SynchronizedEnumerator</c> ensures that the collection is locked for the duration of the enumeration.
    /// This is especially useful to maintain thread safety when iterating over shared collections.
    /// The underlying lock is acquired when the enumerator is instantiated and released when the enumerator is disposed.
    /// </remarks>
    public readonly struct SynchronizedEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private readonly Lock _root;

        public SynchronizedEnumerator(IEnumerator<T> enumerator, Lock root)
        {
            _enumerator = enumerator;
            _root = root;
            
            // entering lock in constructor
            Monitor.Enter(_root);
        }

        public void Dispose()
        {
            _enumerator.Dispose();
            // .. and exiting lock on Dispose()
            // This will be called when foreach loop finishes
            Monitor.Exit(_root);
        }

        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => _enumerator.Reset();

        public T Current => _enumerator.Current;

        object IEnumerator.Current => Current!;
    }
}