using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    /// <summary>
    /// Measures the performance of different strategies for filtering region updates.
    /// This benchmark focuses specifically on the filtering and pool management phase.
    /// </summary>
    [MemoryDiagnoser]
    public class MapRegionPartBenchmark
    {
        private List<IUpdate> _listSomeAccepted = null!;
        private IUpdate[] _arraySomeAccepted = null!;
        private IReadOnlyList<IUpdate> _readOnlyListSomeAccepted = null!;
        private IEnumerable<IUpdate> _lazySomeAccepted = null!;
        private object _character = new object();

        [Params(1, 10, 50)]
        public int Count;

        [GlobalSetup]
        public void Setup()
        {
            _listSomeAccepted = Enumerable.Range(0, Count).Select(i => (IUpdate)new MockUpdate(i % 2 == 0)).ToList();
            _arraySomeAccepted = _listSomeAccepted.ToArray();
            _readOnlyListSomeAccepted = _listSomeAccepted.AsReadOnly();
            _lazySomeAccepted = _listSomeAccepted.Select(x => x);
        }

        // --- Baseline (LINQ) ---

        [Benchmark(Baseline = true)]
        public List<IUpdate> List_Some_Linq() => _listSomeAccepted.Where(u => u.CanUpdateFor(_character)).ToList();

        // --- Production Strategy 1: Concrete List Indexing (Matches List<T> branch) ---

        [Benchmark]
        public int List_Some_ConcreteIndex_ArrayPool()
        {
            List<IUpdate> list = _listSomeAccepted;
            int count = list.Count;
            var buffer = ArrayPool<IUpdate>.Shared.Rent(count);
            try {
                int found = 0;
                for (int i = 0; i < count; i++) {
                    var update = list[i];
                    if (update.CanUpdateFor(_character)) buffer[found++] = update;
                }
                return found;
            } finally { ArrayPool<IUpdate>.Shared.Return(buffer, clearArray: true); }
        }

        // --- Production Strategy 2: Array Indexing (Matches T[] branch) ---

        [Benchmark]
        public int Array_Some_ConcreteIndex_ArrayPool()
        {
            IUpdate[] array = _arraySomeAccepted;
            int count = array.Length;
            var buffer = ArrayPool<IUpdate>.Shared.Rent(count);
            try {
                int found = 0;
                for (int i = 0; i < count; i++) {
                    var update = array[i];
                    if (update.CanUpdateFor(_character)) buffer[found++] = update;
                }
                return found;
            } finally { ArrayPool<IUpdate>.Shared.Return(buffer, clearArray: true); }
        }

        // --- Production Strategy 3: IReadOnlyList Indexing (Matches IReadOnlyList fallback) ---

        [Benchmark]
        public int List_Some_ReadOnlyIndex_ArrayPool()
        {
            IReadOnlyList<IUpdate> readOnlyList = _readOnlyListSomeAccepted;
            int count = readOnlyList.Count;
            var buffer = ArrayPool<IUpdate>.Shared.Rent(count);
            try {
                int found = 0;
                for (int i = 0; i < count; i++) {
                    var update = readOnlyList[i];
                    if (update.CanUpdateFor(_character)) buffer[found++] = update;
                }
                return found;
            } finally { ArrayPool<IUpdate>.Shared.Return(buffer, clearArray: true); }
        }

        // --- Fallback Strategy: Lazy IEnumerable (Matches pure IEnumerable branch) ---

        [Benchmark]
        public List<IUpdate> Lazy_Some_Linq_Fallback() => _lazySomeAccepted.Where(u => u.CanUpdateFor(_character)).ToList();

        public interface IUpdate { bool CanUpdateFor(object character); }
        private class MockUpdate : IUpdate {
            private readonly bool _res;
            public MockUpdate(bool res) => _res = res;
            public bool CanUpdateFor(object character) => _res;
        }
    }
}
