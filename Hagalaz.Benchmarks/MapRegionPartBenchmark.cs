using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    /// <summary>
    /// Measures the filtering overhead and pool management for different region update strategies.
    /// This benchmark focuses specifically on the filtering phase, not the end-to-end network dispatch.
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

        // --- Production Fast Path: List<T> ---

        [Benchmark]
        public int List_Some_Concrete_ArrayPool()
        {
            return FilterAndReturnCount(_listSomeAccepted, _character);
        }

        // --- Production Fast Path: Array ---

        [Benchmark]
        public int Array_Some_Concrete_ArrayPool()
        {
            return FilterAndReturnCount(_arraySomeAccepted, _character);
        }

        // --- Production Fallback: IReadOnlyList (Interface Dispatch) ---

        [Benchmark]
        public int List_Some_Interface_ArrayPool()
        {
            return FilterAndReturnCount(_readOnlyListSomeAccepted, _character);
        }

        // --- Fallback Strategy: Lazy IEnumerable ---

        [Benchmark]
        public List<IUpdate> Lazy_Some_Linq_Fallback() => _lazySomeAccepted.Where(u => u.CanUpdateFor(_character)).ToList();

        private int FilterAndReturnCount<TList>(TList updates, object character) where TList : IReadOnlyList<IUpdate>
        {
            int count = updates.Count;
            if (count == 0) return 0;

            var buffer = ArrayPool<IUpdate>.Shared.Rent(count);
            try
            {
                int found = 0;
                for (int i = 0; i < count; i++)
                {
                    var update = updates[i];
                    if (update.CanUpdateFor(character)) buffer[found++] = update;
                }
                return found;
            }
            finally
            {
                ArrayPool<IUpdate>.Shared.Return(buffer, clearArray: true);
            }
        }

        public interface IUpdate { bool CanUpdateFor(object character); }
        private class MockUpdate : IUpdate {
            private readonly bool _res;
            public MockUpdate(bool res) => _res = res;
            public bool CanUpdateFor(object character) => _res;
        }
    }
}
