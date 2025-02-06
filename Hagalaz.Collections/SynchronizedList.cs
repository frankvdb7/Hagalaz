using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Hagalaz.Collections
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SynchronizedList<T> : IList<T>
    {
        private readonly List<T> _list;

        /// <summary>
        /// 
        /// </summary>
        public SynchronizedList() => _list = new List<T>();

        public object SyncRoot { get; } = new object();

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
    
    public struct SynchronizedEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;
        private readonly object _root;

        public SynchronizedEnumerator(IEnumerator<T> enumerator, object root)
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

        object IEnumerator.Current => Current;
    }
}