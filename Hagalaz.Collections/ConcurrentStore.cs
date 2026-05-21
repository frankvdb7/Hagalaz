using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Collections
{
    public class ConcurrentStore<TKey, TValue> : IEnumerable<TValue> where TKey : notnull where TValue : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _values = new();

        public TValue? this[TKey key]
        {
            get { _values.TryGetValue(key, out var value); return value; }
            set => _values[key] = value ?? throw new ArgumentNullException(nameof(value));
        }

        public int Count => _values.Count;
        public bool TryAdd(TKey key, TValue value) => _values.TryAdd(key, value);
        public bool TryRemove(TKey key) => _values.TryRemove(key, out _);
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _values.TryGetValue(key, out value);
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory) => _values.GetOrAdd(key, valueFactory);
        public TValue? GetOrDefault(TKey key) => _values.GetValueOrDefault(key);
        public bool ContainsKey(TKey key) => _values.ContainsKey(key);

        public int CopyValuesTo(TValue[] array, int index)
        {
            var count = 0;
            foreach (var kvp in _values)
            {
                if (index + count >= array.Length) break;
                array[index + count++] = kvp.Value;
            }
            return count;
        }

        public bool Any(Func<TValue, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            foreach (var kvp in _values) { if (predicate(kvp.Value)) return true; }
            return false;
        }

        public TValue? FirstOrDefault(Func<TValue, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            foreach (var kvp in _values) { if (predicate(kvp.Value)) return kvp.Value; }
            return default;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<TValue>
        {
            private IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;
            public Enumerator(ConcurrentStore<TKey, TValue> concurrentStore) => _enumerator = concurrentStore._values.GetEnumerator();
            public TValue Current => _enumerator.Current.Value;
            object IEnumerator.Current => Current;
            public void Dispose() => _enumerator.Dispose();
            public bool MoveNext() => _enumerator.MoveNext();
            public void Reset() => _enumerator.Reset();
        }
    }
}
