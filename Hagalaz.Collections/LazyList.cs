using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace Hagalaz.Collections
{
    /// <summary>
    /// Provides extension methods for creating <see cref="LazyList{T}"/> instances from enumerable sequences,
    /// enabling deferred evaluation and result caching.
    /// </summary>
    [PublicAPI]
    public static class LazyListExtensions
    {
        /// <summary>
        /// Converts an <see cref="IEnumerable{T}"/> into a <see cref="LazyList{T}"/>,
        /// which defers enumeration of the source sequence until elements are accessed and then caches them.
        /// </summary>
        /// <typeparam name="T">The type of elements in the source sequence.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to convert.</param>
        /// <returns>A new <see cref="LazyList{T}"/> that wraps the source sequence.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="source"/> is null.</exception>
        public static LazyList<T> ToLazyList<T>(this IEnumerable<T> source) => new(source);
    }

    /// <summary>
    /// Represents a thread-safe, lazily-evaluated list that caches its elements as they are enumerated.
    /// The source <see cref="IEnumerable{T}"/> is only enumerated once, and only as far as requested.
    /// This is useful for expensive or non-repeatable sequences.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    [PublicAPI]
    public sealed class LazyList<T> : IEnumerable<T>, IDisposable
    {
        private readonly IList<T> _cache;
        private IEnumerator<T> _sourceEnumerator;
        private bool _allElementsAreCached;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyList{T}"/> class with the specified source sequence.
        /// </summary>
        /// <param name="source">The source <see cref="IEnumerable{T}"/> to be evaluated lazily.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="source"/> is null.</exception>
        /// <remarks>
        /// If the source sequence is already an <see cref="IList{T}"/>, it will be used directly as the cache,
        /// and no further enumeration will occur. Otherwise, a new cache is created, and the source enumerator
        /// will be used to populate it on demand.
        /// </remarks>
        public LazyList(IEnumerable<T> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            _cache = (source as IList<T>)!;
            if (_cache == null!) // Needs caching
            {
                _cache = new List<T>();
                _sourceEnumerator = source.GetEnumerator();
            }
            else // Needs no caching
            {
                _allElementsAreCached = true;
                _sourceEnumerator = Enumerable.Empty<T>().GetEnumerator();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the lazily-evaluated collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the list.</returns>
        public IEnumerator<T> GetEnumerator() => _allElementsAreCached ? _cache.GetEnumerator() : new LazyListEnumerator(this);

        /// <summary>
        /// Returns an enumerator that iterates through the lazily-evaluated collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> that can be used to iterate through the list.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Releases resources used by the <see cref="LazyList{T}"/>, primarily the source enumerator if it was created.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="LazyList{T}"/> class.
        /// </summary>
        ~LazyList() => Dispose(false);

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (_sourceEnumerator == null!)
            {
                return;
            }

            _sourceEnumerator.Dispose();
            _sourceEnumerator = null!;

            // No native resources to free.
        }

        /// <summary>
        /// An enumerator for the LazyList that pulls items from the source enumerator and caches them as needed.
        /// </summary>
        private class LazyListEnumerator : IEnumerator<T>
        {
            private readonly LazyList<T> _lazyList;
            private readonly Lock _lock = new();
            private const int _startIndex = -1;
            private int _index = _startIndex;

            /// <summary>
            /// Initializes a new instance of the <see cref="LazyListEnumerator"/> class.
            /// </summary>
            /// <param name="lazyList">The parent <see cref="LazyList{T}"/> to enumerate.</param>
            public LazyListEnumerator(LazyList<T> lazyList) => _lazyList = lazyList;

            /// <summary>
            /// Advances the enumerator to the next element of the collection, retrieving it from the cache or the source enumerator.
            /// </summary>
            /// <returns><c>true</c> if the enumerator was successfully advanced to the next element; <c>false</c> if the enumerator has passed the end of the collection.</returns>
            public bool MoveNext()
            {
                var result = true;
                _index++;
                if (IndexItemIsInCache) // Double-checked locking pattern for thread-safe lazy evaluation.
                {
                    SetCurrentToIndex();
                }
                else
                {
                    lock (_lock)
                    {
                        if (IndexItemIsInCache)
                        {
                            SetCurrentToIndex();
                        }
                        else
                        {
                            result = !_lazyList._allElementsAreCached && _lazyList._sourceEnumerator.MoveNext();
                            if (result)
                            {
                                Current = _lazyList._sourceEnumerator.Current;
                                _lazyList._cache.Add(_lazyList._sourceEnumerator.Current);
                            }
                            else if (!_lazyList._allElementsAreCached)
                            {
                                _lazyList._allElementsAreCached = true;
                                _lazyList._sourceEnumerator.Dispose();
                            }
                        }
                    }
                }

                return result;
            }

            private bool IndexItemIsInCache => _index < _lazyList._cache.Count;

            private void SetCurrentToIndex() => Current = _lazyList._cache[_index];

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset() => _index = _startIndex;

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            public T Current { get; private set; } = default!;

            /// <summary>
            /// Gets the element at the current position of the enumerator.
            /// </summary>
            object IEnumerator.Current => Current!;

            /// <summary>
            /// Disposes the enumerator. The parent <see cref="LazyList{T}"/> is responsible for disposing the source enumerator.
            /// </summary>
            public void Dispose()
            {
                // The _lazyList._sourceEnumerator
                // is disposed in LazyList
            }
        }
    }
}