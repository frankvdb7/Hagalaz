using BenchmarkDotNet.Attributes;
using Hagalaz.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    public partial class HagalazBenchmarks
    {
        private List<int> _visibleCreaturesList = null!;
        private ListHashSet<int> _visibleCreaturesListHashSet = null!;
        private List<int> _localEntities = null!;
        private List<int> _regionsCharacters = null!;

        private void SetupViewport()
        {
            _visibleCreaturesList = Enumerable.Range(0, N).ToList();
            _visibleCreaturesListHashSet = _visibleCreaturesList.ToListHashSet();
            _localEntities = Enumerable.Range(N / 2, 255).ToList();
            _regionsCharacters = Enumerable.Range(0, N).ToList();
        }

        [Benchmark]
        public int Viewport_Old_List()
        {
            int count = 0;
            foreach (var entity in _localEntities)
                if (_visibleCreaturesList.Contains(entity)) count++;
            return count;
        }

        [Benchmark]
        public int Viewport_New_ListHashSet()
        {
            int count = 0;
            foreach (var entity in _localEntities)
                if (_visibleCreaturesListHashSet.Contains(entity)) count++;
            return count;
        }

        [Benchmark]
        public int ViewportUpdateTick_Linq()
        {
            var visibleCreatures = new ListHashSet<int>();
            foreach (var character in _regionsCharacters.Where(c => c % 2 == 0))
            {
                visibleCreatures.Add(character);
            }
            return visibleCreatures.Count;
        }

        [Benchmark]
        public int ViewportUpdateTick_Manual()
        {
            var visibleCreatures = new ListHashSet<int>(255);
            foreach (var character in _regionsCharacters)
            {
                if (character % 2 == 0)
                {
                    visibleCreatures.Add(character);
                }
            }
            return visibleCreatures.Count;
        }

        [Benchmark(OperationsPerInvoke = 100)]
        public int ViewportTypedAccess_Cast_Baseline()
        {
            // Simulates OfType<T>().ToListHashSet()
            int total = 0;
            for (int i = 0; i < 100; i++)
            {
                var visibleCreatures = new List<object>(_regionsCharacters.Cast<object>());
                var result = visibleCreatures.OfType<int>().ToListHashSet();
                total += result.Count;
            }
            return total;
        }

        [Benchmark(OperationsPerInvoke = 100)]
        public int ViewportTypedAccess_Direct_Optimized()
        {
            // Simulates direct access to pre-maintained typed collection
            int total = 0;
            for (int i = 0; i < 100; i++)
            {
                var visibleNpcs = _visibleCreaturesListHashSet;
                total += visibleNpcs.Count;
            }
            return total;
        }
    }
}
