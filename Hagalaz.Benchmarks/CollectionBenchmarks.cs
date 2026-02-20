using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Hagalaz.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    [MemoryDiagnoser]
    public class CollectionBenchmarks
    {
        private List<int> _list;
        private ListHashSet<int> _listHashSet;
        private int _lookupValue;

        [Params(10, 100, 1000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            _list = Enumerable.Range(0, N).ToList();
            _listHashSet = _list.ToListHashSet();
            _lookupValue = N - 1; // Worst case for List
        }

        [Benchmark]
        public bool ListContains() => _list.Contains(_lookupValue);

        [Benchmark]
        public bool ListHashSetContains() => _listHashSet.Contains(_lookupValue);

        [Benchmark]
        public int ListIndexer() => _list[N / 2];

        [Benchmark]
        public int ListHashSetIndexer() => _listHashSet[N / 2];
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<CollectionBenchmarks>();
            BenchmarkRunner.Run<ViewportBenchmarks>();
        }
    }
}
