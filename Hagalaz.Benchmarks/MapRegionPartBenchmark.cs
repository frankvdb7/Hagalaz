using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    /// <summary>
    /// Measures the filtering overhead and pool management for the optimized MapRegionPart update strategy.
    /// This benchmark focuses specifically on the filtering phase, as network dispatch overhead is constant.
    /// </summary>
    [MemoryDiagnoser]
    public class MapRegionPartBenchmark
    {
        private List<IUpdate> _listSomeAccepted = null!;
        private IReadOnlyList<IUpdate> _readOnlyListSomeAccepted = null!;
        private IEnumerable<IUpdate> _lazySomeAccepted = null!;
        private object _character = new object();

        [Params(1, 10, 50)]
        public int Count;

        [GlobalSetup]
        public void Setup()
        {
            _listSomeAccepted = Enumerable.Range(0, Count).Select(i => (IUpdate)new MockUpdate(i % 2 == 0)).ToList();
            _readOnlyListSomeAccepted = _listSomeAccepted.AsReadOnly();
            _lazySomeAccepted = _listSomeAccepted.Select(x => x);
        }

        // --- Baseline ---

        /// <summary>
        /// Represents the original production implementation using LINQ.
        /// </summary>
        [Benchmark(Baseline = true)]
        public int Baseline_Linq_ToList()
        {
            return _listSomeAccepted.Where(u => u.CanUpdateFor(_character)).ToList().Count;
        }

        // --- Production Optimized Path ---

        /// <summary>
        /// Represents the new optimized path taking IReadOnlyList (interface dispatch).
        /// This is what production uses for _updates (List) and full updates.
        /// </summary>
        [Benchmark]
        public int Production_Optimized_IReadOnlyList()
        {
            return FilterUsingPool(_readOnlyListSomeAccepted, _character);
        }

        // --- Production Fallback Path ---

        /// <summary>
        /// Represents the fallback path for pure IEnumerable.
        /// </summary>
        [Benchmark]
        public int Production_Fallback_IEnumerable()
        {
            return _lazySomeAccepted.Where(u => u.CanUpdateFor(_character)).ToList().Count;
        }

        private int FilterUsingPool(IReadOnlyList<IUpdate> updates, object character)
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
                    if (update.CanUpdateFor(character))
                    {
                        buffer[found++] = update;
                    }
                }
                // In production, we would iterate 'buffer' here to send messages.
                return found;
            }
            finally
            {
                ArrayPool<IUpdate>.Shared.Return(buffer, clearArray: true);
            }
        }

        public interface IUpdate { bool CanUpdateFor(object character); }
        private class MockUpdate : IUpdate
        {
            private readonly bool _res;
            public MockUpdate(bool res) => _res = res;
            public bool CanUpdateFor(object character) => _res;
        }
    }
}
