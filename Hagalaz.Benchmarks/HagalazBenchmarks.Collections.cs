using BenchmarkDotNet.Attributes;
using Hagalaz.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    public partial class HagalazBenchmarks
    {
        private List<int> _list = null!;
        private ListHashSet<int> _listHashSet = null!;
        private int _lookupValue;
        private ConcurrentStore<int, int> _concurrentStore = null!;
        private int[][] _jaggedArrays = null!;

        private void SetupCollections()
        {
            _list = Enumerable.Range(0, N).ToList();
            _listHashSet = _list.ToListHashSet();
            _lookupValue = N - 1;

            _concurrentStore = new ConcurrentStore<int, int>();
            for (int i = 0; i < N; i++) _concurrentStore.TryAdd(i, i);

            _jaggedArrays = new int[5][]
            {
                Enumerable.Range(0, N / 5).ToArray(),
                Enumerable.Range(0, N / 5).ToArray(),
                Enumerable.Range(0, N / 5).ToArray(),
                Enumerable.Range(0, N / 5).ToArray(),
                Enumerable.Range(0, N / 5).ToArray()
            };
        }

        [Benchmark]
        public bool ListContains() => _list.Contains(_lookupValue);

        [Benchmark]
        public bool ListHashSetContains() => _listHashSet.Contains(_lookupValue);

        [Benchmark]
        public int ConcurrentStoreIteration()
        {
            int sum = 0;
            foreach (var value in _concurrentStore)
            {
                sum += value;
            }
            return sum;
        }

        [Benchmark]
        public int ListHashSetIteration()
        {
            int sum = 0;
            foreach (var value in _listHashSet)
            {
                sum += value;
            }
            return sum;
        }

        [Benchmark]
        public ListHashSet<int> ToListHashSet_Benchmark() => _list.ToListHashSet();

        /// <summary>
        /// Benchmarks the IndexOf extension method for IEnumerable.
        /// </summary>
        [Benchmark]
        public int EnumerableIndexOf() => Hagalaz.Collections.Extensions.CollectionExtensions.IndexOf(_list, i => i == _lookupValue);

        /// <summary>
        /// Benchmarks the AddRange extension method for HashSet.
        /// </summary>
        [Benchmark]
        public HashSet<int> HashSetAddRange()
        {
            var set = new HashSet<int>();
            Hagalaz.Collections.Extensions.CollectionExtensions.AddRange(set, _list);
            return set;
        }

        /// <summary>
        /// Benchmarks the ForEach extension method for IEnumerable.
        /// </summary>
        [Benchmark]
        public int EnumerableForEach()
        {
            int sum = 0;
            Hagalaz.Collections.Extensions.CollectionExtensions.ForEach(_list, i => sum += i);
            return sum;
        }

        /// <summary>
        /// Benchmarks the MakeArray utility method.
        /// </summary>
        [Benchmark]
        public int[] ArrayUtilities_MakeArray() => Hagalaz.Utilities.ArrayUtilities.MakeArray(_jaggedArrays);
    }
}
