using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Collections
{
    /// <summary>
    /// Represents a thread-safe collection of key-value pairs that can be accessed by multiple threads concurrently.
    /// This class is a lightweight wrapper around <see cref="ConcurrentDictionary{TKey, TValue}"/>, providing a simplified interface
    /// and exposing an enumerator for its values.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of the values in the collection.</typeparam>
    public class ConcurrentStore<TKey, TValue> : IEnumerable<TValue> where TKey : notnull where TValue : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _values = new();

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get or set.</param>
        /// <returns>The value associated with the specified key. If the key is not found, a get operation returns the default value for the type.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the value being set is null.</exception>
        public TValue? this[TKey key]
        {
            get
            {
                _values.TryGetValue(key, out var value);
                return value;
            }
            set => _values[key] = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets the number of key-value pairs contained in the <see cref="ConcurrentStore{TKey, TValue}"/>.
        /// </summary>
        public int Count => _values.Count;

        /// <summary>
        /// Attempts to add the specified key and value to the store.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns><c>true</c> if the key-value pair was added successfully; otherwise, <c>false</c> if the key already exists.</returns>
        public bool TryAdd(TKey key, TValue value) => _values.TryAdd(key, value);

        /// <summary>
        /// Attempts to remove the value with the specified key from the store.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns><c>true</c> if the element was removed successfully; otherwise, <c>false</c>.</returns>
        public bool TryRemove(TKey key) => _values.TryRemove(key, out _);

        /// <summary>
        /// Attempts to get the value associated with the specified key from the store.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
        /// <returns><c>true</c> if the key was found in the store; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _values.TryGetValue(key, out value);

        /// <summary>
        /// Adds a key-value pair to the store if the key does not already exist, or returns the existing value if the key exists.
        /// </summary>
        /// <param name="key">The key of the element to add or retrieve.</param>
        /// <param name="valueFactory">The function used to generate a value for the key if the key does not already exist.</param>
        /// <returns>The value for the key. This will be either the existing value for the key if the key is already in the dictionary, or the new value if the key was not in the dictionary.</returns>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory) => _values.GetOrAdd(key, valueFactory);

        /// <summary>
        /// Returns the value associated with the specified key, or the default value if the key does not exist.
        /// </summary>
        /// <param name="key">The key of the element to retrieve.</param>
        /// <returns>The value associated with the key, or the default value of <typeparamref name="TValue"/> if the key is not found.</returns>
        public TValue? GetOrDefault(TKey key) => _values.GetValueOrDefault(key);

        /// <summary>
        /// Determines whether the store contains the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the store.</param>
        /// <returns><c>true</c> if the store contains an element with the specified key; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(TKey key) => _values.ContainsKey(key);

        /// <summary>
        /// Returns an enumerator that iterates through the values in the collection.
        /// </summary>
        /// <returns>An enumerator for the values in the collection.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Returns an enumerator that iterates through the values in the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through the values in the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// A private enumerator for iterating over the values of the ConcurrentStore.
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            private IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> struct.
            /// </summary>
            /// <param name="concurrentStore">The concurrent store to enumerate.</param>
            public Enumerator(ConcurrentStore<TKey, TValue> concurrentStore) => _enumerator = concurrentStore._values.GetEnumerator();

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public TValue Current => _enumerator.Current.Value;

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            object IEnumerator.Current => Current;

            /// <summary>
            /// Releases all resources used by the <see cref="Enumerator"/>.
            /// </summary>
            public void Dispose() => _enumerator.Dispose();

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns><c>true</c> if the enumerator was successfully advanced to the next element; <c>false</c> if the enumerator has passed the end of the collection.</returns>
            public bool MoveNext() => _enumerator.MoveNext();

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset() => _enumerator.Reset();
        }
    }
}
