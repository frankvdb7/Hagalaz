using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    [MemoryDiagnoser]
    public class MapRegionPartBenchmark
    {
        private List<IUpdate> _listSomeAccepted = null!;
        private List<IUpdate> _listAllAccepted = null!;
        private List<IUpdate> _listNoneAccepted = null!;
        private IUpdate[] _arraySomeAccepted = null!;
        private ReadOnlyCollection<IUpdate> _readOnlyCollectionSomeAccepted = null!;
        private IEnumerable<IUpdate> _lazySomeAccepted = null!;
        private object _character = new object();

        [Params(1, 10, 50)]
        public int Count;

        [GlobalSetup]
        public void Setup()
        {
            _listSomeAccepted = Enumerable.Range(0, Count).Select(i => (IUpdate)new MockUpdate(i % 2 == 0)).ToList();
            _listAllAccepted = Enumerable.Range(0, Count).Select(_ => (IUpdate)new MockUpdate(true)).ToList();
            _listNoneAccepted = Enumerable.Range(0, Count).Select(_ => (IUpdate)new MockUpdate(false)).ToList();

            _arraySomeAccepted = _listSomeAccepted.ToArray();
            _readOnlyCollectionSomeAccepted = new ReadOnlyCollection<IUpdate>(_listSomeAccepted);
            _lazySomeAccepted = _listSomeAccepted.Select(x => x);
        }

        // --- Strategies for List<T> ---

        [Benchmark(Baseline = true)]
        public List<IUpdate> List_Some_Linq() => _listSomeAccepted.Where(u => u.CanUpdateFor(_character)).ToList();

        [Benchmark]
        public int List_Some_ArrayPool()
        {
            int count = _listSomeAccepted.Count;
            var buffer = ArrayPool<IUpdate>.Shared.Rent(count);
            try {
                int found = 0;
                for (int i = 0; i < count; i++) {
                    var update = _listSomeAccepted[i];
                    if (update.CanUpdateFor(_character)) buffer[found++] = update;
                }
                return found;
            } finally { ArrayPool<IUpdate>.Shared.Return(buffer, clearArray: true); }
        }

        // --- Other Shapes (Using IReadOnlyList path) ---

        [Benchmark]
        public int Array_Some_ArrayPool()
        {
            int count = _arraySomeAccepted.Length;
            var buffer = ArrayPool<IUpdate>.Shared.Rent(count);
            try {
                int found = 0;
                for (int i = 0; i < count; i++) {
                    var update = _arraySomeAccepted[i];
                    if (update.CanUpdateFor(_character)) buffer[found++] = update;
                }
                return found;
            } finally { ArrayPool<IUpdate>.Shared.Return(buffer, clearArray: true); }
        }

        [Benchmark]
        public int ReadOnlyCollection_Some_ArrayPool()
        {
            int count = _readOnlyCollectionSomeAccepted.Count;
            var buffer = ArrayPool<IUpdate>.Shared.Rent(count);
            try {
                int found = 0;
                for (int i = 0; i < count; i++) {
                    var update = _readOnlyCollectionSomeAccepted[i];
                    if (update.CanUpdateFor(_character)) buffer[found++] = update;
                }
                return found;
            } finally { ArrayPool<IUpdate>.Shared.Return(buffer, clearArray: true); }
        }

        // --- Edge Cases ---

        [Benchmark]
        public int List_None_ArrayPool()
        {
            int count = _listNoneAccepted.Count;
            var buffer = ArrayPool<IUpdate>.Shared.Rent(count);
            try {
                int found = 0;
                for (int i = 0; i < count; i++) {
                    var update = _listNoneAccepted[i];
                    if (update.CanUpdateFor(_character)) buffer[found++] = update;
                }
                return found;
            } finally { ArrayPool<IUpdate>.Shared.Return(buffer, clearArray: true); }
        }

        [Benchmark]
        public int List_All_ArrayPool()
        {
            int count = _listAllAccepted.Count;
            var buffer = ArrayPool<IUpdate>.Shared.Rent(count);
            try {
                int found = 0;
                for (int i = 0; i < count; i++) {
                    var update = _listAllAccepted[i];
                    if (update.CanUpdateFor(_character)) buffer[found++] = update;
                }
                return found;
            } finally { ArrayPool<IUpdate>.Shared.Return(buffer, clearArray: true); }
        }

        // --- Fallback ---

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
