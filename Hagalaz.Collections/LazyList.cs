using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace Hagalaz.Collections
{
    /// <summary>
    /// Provides extension methods for working with <see cref="LazyList{T}"/> to enable deferred evaluation and caching when working with enumerable sequences.
    /// </summary>
    [PublicAPI]
    public static class LazyListExtensions
    {
        /// <summary>
        /// Converts the given enumerable source to a <see cref="LazyList{T}"/> which allows
        /// deferred evaluation and caching of its elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the enumerable source.</typeparam>
        /// <param name="source">The enumerable source to be converted to a lazy list.</param>
        /// <returns>A <see cref="LazyList{T}"/> wrapping the given enumerable source.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided source is null.</exception>
        public static LazyList<T> ToLazyList<T>(this IEnumerable<T> source) => new(source);
    }

    /// <summary>
    /// Represents a lazily evaluated list that defers the computation or retrieval of its elements until they are accessed.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    [PublicAPI]
    public sealed class LazyList<T> : IEnumerable<T>, IDisposable
    {
        private readonly IList<T> _cache;
        private IEnumerator<T> _sourceEnumerator;
        private bool _allElementsAreCached;

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

        public IEnumerator<T> GetEnumerator() => _allElementsAreCached ? _cache.GetEnumerator() : new LazyListEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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

        private class LazyListEnumerator : IEnumerator<T>
        {
            private readonly LazyList<T> _lazyList;
            private readonly Lock _lock = new();
            private const int _startIndex = -1;
            private int _index = _startIndex;

            public LazyListEnumerator(LazyList<T> lazyList) => _lazyList = lazyList;

            public bool MoveNext()
            {
                var result = true;
                _index++;
                if (IndexItemIsInCache) //  Double-checked locking pattern
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

            public void Reset() => _index = _startIndex;

            public T Current { get; private set; } = default!;

            object IEnumerator.Current => Current!;

            public void Dispose()
            {
                // The _lazyList._sourceEnumerator
                // is disposed in LazyList
            }
        }
    }
}