using BenchmarkDotNet.Attributes;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Utilities.Model.Creatures;
using System;

namespace Hagalaz.Benchmarks
{
    public partial class HagalazBenchmarks
    {
        private ILocation _loc1 = null!;
        private ILocation _loc2 = null!;

        private void SetupCreatureBenchmarks()
        {
            _loc1 = new Location(100, 100, 0, 0);
            _loc2 = new Location(115, 100, 0, 0); // Distance 15
        }

        [Benchmark]
        public bool CreatureWithinRange_1x1_WorstCase()
        {
            return CreatureRangeHelper.IsWithinRange(_loc1, 1, _loc2, 1, 5);
        }

        [Benchmark]
        public bool CreatureWithinRange_3x3_WorstCase()
        {
            return CreatureRangeHelper.IsWithinRange(_loc1, 3, _loc2, 3, 5);
        }
    }
}
