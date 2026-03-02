using BenchmarkDotNet.Attributes;
using Hagalaz.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    public partial class HagalazBenchmarks
    {
        private string _csvInts = string.Empty;
        private string _csvBools = string.Empty;
        private int[] _intArray = null!;
        private bool[] _boolArray = null!;

        private void SetupStringParsing()
        {
            _csvInts = string.Join(",", Enumerable.Range(0, N));
            _csvBools = string.Join(",", Enumerable.Range(0, N).Select(i => i % 2 == 0 ? "1" : "0"));
            _intArray = Enumerable.Range(0, N).ToArray();
            _boolArray = Enumerable.Range(0, N).Select(i => i % 2 == 0).ToArray();
        }

        [Benchmark]
        public List<int> SelectIntFromString() => StringUtilities.SelectIntFromString(_csvInts).ToList();

        [Benchmark]
        public bool[] DecodeBoolValues() => StringUtilities.DecodeValues(_csvBools);

        [Benchmark]
        public int[] DecodeIntValues_StringDelegate() => StringUtilities.DecodeValues<int>(_csvInts, (string s) => int.Parse(s));

        [Benchmark]
        public int[] DecodeIntValues_SpanDelegate() => StringUtilities.DecodeValuesFromSpan<int>(_csvInts, (ReadOnlySpan<char> segment) => int.Parse(segment, NumberStyles.Any, CultureInfo.InvariantCulture));

        [Benchmark]
        public string EncodeIntValues() => StringUtilities.EncodeValues(_intArray);

        [Benchmark]
        public string EncodeBoolValues() => StringUtilities.EncodeValues(_boolArray);
    }
}
