using BenchmarkDotNet.Attributes;
using Hagalaz.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hagalaz.Benchmarks.StringParsing
{
    [MemoryDiagnoser]
    public class StringParsingBenchmarks
    {
        private string _csvInts = string.Empty;
        private string _csvBools = string.Empty;

        [Params(10, 100)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            _csvInts = string.Join(",", Enumerable.Range(0, N));
            _csvBools = string.Join(",", Enumerable.Range(0, N).Select(i => i % 2 == 0 ? "1" : "0"));
        }

        [Benchmark]
        public List<int> SelectIntFromString() => StringUtilities.SelectIntFromString(_csvInts).ToList();

        [Benchmark]
        public bool[] DecodeBoolValues() => StringUtilities.DecodeValues(_csvBools);

        [Benchmark]
        public int[] DecodeIntValues() => StringUtilities.DecodeValues(_csvInts, int.Parse);
    }
}
