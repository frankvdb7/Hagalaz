using BenchmarkDotNet.Attributes;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Utilities.Model.Creatures;
using System;

namespace Hagalaz.Benchmarks
{
    public partial class HagalazBenchmarks
    {
        private Location _loc1;
        private Location _loc2;

        private void SetupCreatureBenchmarks()
        {
            _loc1 = new Location(100, 100, 0, 0);
            _loc2 = new Location(115, 100, 0, 0); // Distance 15
        }

        [Benchmark]
        public bool CreatureWithinRange_1x1_WorstCase_v2()
        {
            return CreatureRangeHelper.IsWithinRange<Location, Location>(_loc1, 1, _loc2, 1, 5);
        }

        [Benchmark]
        public bool CreatureWithinRange_3x3_WorstCase_v2()
        {
            return CreatureRangeHelper.IsWithinRange<Location, Location>(_loc1, 3, _loc2, 3, 5);
        }
    }
}
