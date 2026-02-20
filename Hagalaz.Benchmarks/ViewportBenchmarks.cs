using BenchmarkDotNet.Attributes;
using Hagalaz.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    [MemoryDiagnoser]
    public class ViewportBenchmarks
    {
        private List<int> _visibleCreaturesList;
        private ListHashSet<int> _visibleCreaturesListHashSet;
        private List<int> _localEntities;

        [GlobalSetup]
        public void Setup()
        {
            _visibleCreaturesList = Enumerable.Range(0, 1000).ToList();
            _visibleCreaturesListHashSet = _visibleCreaturesList.ToListHashSet();
            _localEntities = Enumerable.Range(500, 255).ToList();
        }

        [Benchmark(Baseline = true)]
        public int EncoderSimulation_Old_List()
        {
            int count = 0;
            foreach (var entity in _localEntities)
                if (_visibleCreaturesList.Contains(entity)) count++;
            return count;
        }

        [Benchmark]
        public int EncoderSimulation_New_ListHashSet()
        {
            int count = 0;
            foreach (var entity in _localEntities)
                if (_visibleCreaturesListHashSet.Contains(entity)) count++;
            return count;
        }
    }
}
