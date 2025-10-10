using System.Collections;
using System.Collections.Generic;
using System.Threading;
#pragma warning disable CS9216 // A value of type 'System.Threading.Lock' converted to a different type will use likely unintended monitor-based locking in 'lock' statement

namespace Hagalaz.Collections
{
    /// <summary>
    /// Provides a thread-safe wrapper around a <see cref="List{T}"/>, ensuring that all operations
    /// are synchronized and can be safely accessed from multiple threads.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <remarks>
    /// This class uses a lock on a <see cref="SyncRoot"/> object for all its operations to prevent race conditions.
    /// When enumerating an instance of this list, a lock is held for the entire duration of the enumeration
    /// to ensure the collection is not modified.
    /// </remarks>
    public class SynchronizedList<T> : IList<T>
    {
        private readonly List<T> _list;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedList{T}"/> class that is empty and has the default initial capacity.
        /// </summary>
        public SynchronizedList() => _list = [];

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="SynchronizedList{T}"/>.
        /// </summary>
        public Lock SyncRoot { get; } = new();

        /// <summary>
        /// Gets the number of elements contained in the <see cref="SynchronizedList{T}"/>.
        /// </summary>
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

        /// <summary>
        /// Gets a value indicating whether the <see cref="SynchronizedList{T}"/> is read-only. This implementation always returns <c>false</c>.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds an object to the end of the <see cref="SynchronizedList{T}"/>. This operation is thread-safe.
        /// </summary>
        /// <param name="item">The object to be added to the end of the list.</param>
        public void Add(T item)
        {
            lock (SyncRoot)
            {
                _list.Add(item);
            }
        }

        /// <summary>
        /// Removes all elements from the <see cref="SynchronizedList{T}"/>. This operation is thread-safe.
        /// </summary>
        public void Clear()
        {
            lock (SyncRoot)
            {
                _list.Clear();
            }
        }

        /// <summary>
        /// Determines whether an element is in the <see cref="SynchronizedList{T}"/>. This operation is thread-safe.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <returns><c>true</c> if the item is found in the list; otherwise, <c>false</c>.</returns>
        public bool Contains(T item)
        {
            lock (SyncRoot)
            {
                return _list.Contains(item);
            }
        }

        /// <summary>
        /// Copies the entire <see cref="SynchronizedList{T}"/> to a compatible one-dimensional array, starting at the specified index of the target array. This operation is thread-safe.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from the list.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (SyncRoot)
            {
                _list.CopyTo(array, arrayIndex);
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="SynchronizedList{T}"/>. This operation is thread-safe.
        /// </summary>
        /// <param name="item">The object to remove from the list.</param>
        /// <returns><c>true</c> if the item was successfully removed; otherwise, <c>false</c>.</returns>
        public bool Remove(T item)
        {
            lock (SyncRoot)
            {
                return _list.Remove(item);
            }
        }

        /// <summary>
        /// Returns a thread-safe enumerator that iterates through the <see cref="SynchronizedList{T}"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> for the list.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (SyncRoot)
            {
                return new SynchronizedEnumerator<T>(_list.GetEnumerator(), SyncRoot);
            }
        }

        /// <summary>
        /// Returns a thread-safe enumerator that iterates through the <see cref="SynchronizedList{T}"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> for the list.</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            lock (SyncRoot)
            {
                return new SynchronizedEnumerator<T>(_list.GetEnumerator(), SyncRoot);
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified index. Both get and set operations are thread-safe.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
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

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the entire <see cref="SynchronizedList{T}"/>. This operation is thread-safe.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <returns>The zero-based index of the first occurrence of item, if found; otherwise, –1.</returns>
        public int IndexOf(T item)
        {
            lock (SyncRoot)
            {
                return _list.IndexOf(item);
            }
        }

        /// <summary>
        /// Inserts an element into the <see cref="SynchronizedList{T}"/> at the specified index. This operation is thread-safe.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert.</param>
        public void Insert(int index, T item)
        {
            lock (SyncRoot)
            {
                _list.Insert(index, item);
            }
        }

        /// <summary>
        /// Removes the element at the specified index of the <see cref="SynchronizedList{T}"/>. This operation is thread-safe.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            lock (SyncRoot)
            {
                _list.RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// Provides a thread-safe enumerator for a collection by holding a lock during the entire enumeration.
    /// </summary>
    /// <typeparam name="T">The type of elements to enumerate.</typeparam>
    /// <remarks>
    /// This enumerator acquires a lock on the sync root of the collection when it is created and releases
    /// the lock when it is disposed. This prevents the collection from being modified while it is being enumerated.
    /// </remarks>
    public readonly struct SynchronizedEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private readonly Lock _root;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizedEnumerator{T}"/> struct.
        /// The constructor acquires a lock on the provided sync root.
        /// </summary>
        /// <param name="enumerator">The underlying enumerator from the collection.</param>
        /// <param name="root">The synchronization lock object.</param>
        public SynchronizedEnumerator(IEnumerator<T> enumerator, Lock root)
        {
            _enumerator = enumerator;
            _root = root;
            
            // entering lock in constructor
            Monitor.Enter(_root);
        }

        /// <summary>
        /// Releases the lock held by the enumerator and disposes the underlying enumerator.
        /// </summary>
        public void Dispose()
        {
            _enumerator.Dispose();
            // .. and exiting lock on Dispose()
            // This will be called when foreach loop finishes
            Monitor.Exit(_root);
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns><c>true</c> if the enumerator was successfully advanced to the next element; <c>false</c> if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext() => _enumerator.MoveNext();

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset() => _enumerator.Reset();

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public T Current => _enumerator.Current;

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        object IEnumerator.Current => Current!;
    }
}