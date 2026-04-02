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

        [Benchmark]
        public int ViewportTypedAccess_Old()
        {
            // Simulates OfType<T>().ToListHashSet()
            var visibleCreatures = new List<object>(_regionsCharacters.Cast<object>());
            var result = visibleCreatures.OfType<int>().ToListHashSet();
            return result.Count;
        }

        [Benchmark]
        public int ViewportTypedAccess_New()
        {
            // Simulates direct access to pre-maintained typed collection.
            // We iterate to bring the measurement above the noise floor and avoid 0ns results
            // which trigger infinite ratio alerts in CI.
            int sum = 0;
            for (int i = 0; i < 100; i++)
            {
                sum += _visibleCreaturesListHashSet.Count;
            }
            return sum;
        }
    }
}
