using BenchmarkDotNet.Attributes;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    [MemoryDiagnoser]
    public class MapRegionPartBenchmark
    {
        private List<IUpdate> _updates = null!;
        private object _character = new object();

        [Params(1, 10, 50)]
        public int UpdateCount;

        [GlobalSetup]
        public void Setup()
        {
            _updates = new List<IUpdate>();
            for (int i = 0; i < UpdateCount; i++)
            {
                _updates.Add(new MockUpdate());
            }
        }

        [Benchmark(Baseline = true)]
        public List<IUpdate> Old_FilterUpdates()
        {
            return _updates.Where(update => update.CanUpdateFor(_character)).ToList();
        }

        [Benchmark]
        public int New_FilterUpdates_ArrayPool_Clear()
        {
            int updateCount = _updates.Count;
            if (updateCount == 0) return 0;

            var buffer = ArrayPool<IUpdate>.Shared.Rent(updateCount);
            try
            {
                int updateableCount = 0;
                for (int i = 0; i < updateCount; i++)
                {
                    var update = _updates[i];
                    if (update.CanUpdateFor(_character))
                    {
                        buffer[updateableCount++] = update;
                    }
                }

                for (int i = 0; i < updateableCount; i++)
                {
                    var update = buffer[i];
                    update.OnUpdatedFor(_character);
                }

                return updateableCount;
            }
            finally
            {
                ArrayPool<IUpdate>.Shared.Return(buffer, clearArray: true);
            }
        }

        [Benchmark]
        public int New_FilterUpdates_ArrayPool_NoClear()
        {
            int updateCount = _updates.Count;
            if (updateCount == 0) return 0;

            var buffer = ArrayPool<IUpdate>.Shared.Rent(updateCount);
            try
            {
                int updateableCount = 0;
                for (int i = 0; i < updateCount; i++)
                {
                    var update = _updates[i];
                    if (update.CanUpdateFor(_character))
                    {
                        buffer[updateableCount++] = update;
                    }
                }

                for (int i = 0; i < updateableCount; i++)
                {
                    var update = buffer[i];
                    update.OnUpdatedFor(_character);
                }

                return updateableCount;
            }
            finally
            {
                ArrayPool<IUpdate>.Shared.Return(buffer, clearArray: false);
            }
        }

        public interface IUpdate
        {
            bool CanUpdateFor(object character);
            void OnUpdatedFor(object character);
        }

        private class MockUpdate : IUpdate
        {
            public bool CanUpdateFor(object character) => true;
            public void OnUpdatedFor(object character) { }
        }
    }
}
