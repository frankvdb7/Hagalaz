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

        private void SetupCollections()
        {
            _list = Enumerable.Range(0, N).ToList();
            _listHashSet = _list.ToListHashSet();
            _lookupValue = N - 1;

            _concurrentStore = new ConcurrentStore<int, int>();
            for (int i = 0; i < N; i++) _concurrentStore.TryAdd(i, i);
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
        public ListHashSet<int> ToListHashSet_Benchmark() => _list.ToListHashSet();
    }
}
