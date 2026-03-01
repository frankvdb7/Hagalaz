using System.Collections;
using System.Collections.Generic;

namespace Hagalaz.Collections
{
    /// <summary>
    /// A collection that maintains both a list and a hash set to provide O(1) lookups and O(1) indexing.
    /// Note: This collection is optimized for Add, Clear, and Contains. Remove is O(N).
    /// </summary>
    public class ListHashSet<T> : IReadOnlyList<T>, ICollection<T> where T : notnull
    {
        private readonly List<T> _list;
        private readonly HashSet<T> _set;

        public ListHashSet() : this(0) { }

        public ListHashSet(int capacity)
        {
            _list = new List<T>(capacity);
            _set = new HashSet<T>(capacity);
        }

        public T this[int index] => _list[index];

        public int Count => _list.Count;

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            if (_set.Add(item))
            {
                _list.Add(item);
            }
        }

        public void Clear()
        {
            _list.Clear();
            _set.Clear();
        }

        public bool Contains(T item) => _set.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        public bool Remove(T item)
        {
            if (_set.Remove(item))
            {
                return _list.Remove(item);
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
