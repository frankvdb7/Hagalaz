using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Collections
{
    public static class LazyListExtensions
    {
        public static LazyList<T> ToLazyList<T>(this IEnumerable<T> source) => new LazyList<T>(source);
    }
    
    public class LazyList<T> : IEnumerable<T>, IDisposable
    {
        private readonly IList<T> _cache;
        private IEnumerator<T> _sourceEnumerator;
        private bool _allElementsAreCached;

        public LazyList(IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            _cache = source as IList<T>;
            if (_cache == null) // Needs caching
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

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_sourceEnumerator != null)
                {
                    _sourceEnumerator.Dispose();
                    _sourceEnumerator = null;
                }
            }

            // No native resources to free.
        }

        private class LazyListEnumerator : IEnumerator<T>
        {
            private readonly LazyList<T> _lazyList;
            private readonly object _lock = new object();
            private const int StartIndex = -1;
            private int _index = StartIndex;

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

            public void Reset() => _index = StartIndex;

            public T Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                // The _lazyList._sourceEnumerator
                // is disposed in LazyList
            }
        }
    }
}