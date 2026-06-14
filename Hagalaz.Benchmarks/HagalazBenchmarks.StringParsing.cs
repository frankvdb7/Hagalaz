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
        private string _csvDoubles = string.Empty;
        private string _csvBools = string.Empty;
        private int[] _intArray = null!;
        private bool[] _boolArray = null!;
        private string _sourceString = string.Empty;
        private string _beginMarker = "[[[";
        private string _endMarker = "]]]";

        private void SetupStringParsing()
        {
            _csvInts = string.Join(",", Enumerable.Range(0, N));
            _csvDoubles = string.Join(",", Enumerable.Range(0, N).Select(i => (i + 0.5).ToString(CultureInfo.InvariantCulture)));
            _csvBools = string.Join(",", Enumerable.Range(0, N).Select(i => i % 2 == 0 ? "1" : "0"));
            _intArray = Enumerable.Range(0, N).ToArray();
            _boolArray = Enumerable.Range(0, N).Select(i => i % 2 == 0).ToArray();
            _sourceString = "Some prefix " + _beginMarker + new string('A', N) + _endMarker + " Some suffix";
        }

        [Benchmark]
        public string[] GetStringInBetween() => StringUtilities.GetStringInBetween(_beginMarker, _endMarker, _sourceString, false, false);

        [Benchmark]
        public List<int> SelectIntFromString() => StringUtilities.SelectIntFromString(_csvInts).ToList();

        [Benchmark]
        public List<double> SelectDoubleFromString() => StringUtilities.SelectDoubleFromString(_csvDoubles).ToList();

        [Benchmark]
        public List<bool> SelectBoolFromString() => StringUtilities.SelectBoolFromString(_csvBools).ToList();

        [Benchmark]
        public int[] DecodeIntValues() => StringUtilities.DecodeIntValues(_csvInts);

        [Benchmark]
        public double[] DecodeDoubleValues() => StringUtilities.DecodeDoubleValues(_csvDoubles);

        [Benchmark]
        public bool[] DecodeBoolValues() => StringUtilities.DecodeValues(_csvBools);

        [Benchmark]
        public int[] DecodeIntValues_StringDelegate() => StringUtilities.DecodeValues<int>(_csvInts, (string s) => int.Parse(s));

        [Benchmark]
        public int[] DecodeIntValues_SpanDelegate() => StringUtilities.DecodeValuesFromSpan<int>(_csvInts, (ReadOnlySpan<char> segment) => int.Parse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture));

        [Benchmark]
        public string EncodeIntValues() => StringUtilities.EncodeValues(_intArray);

        [Benchmark]
        public string EncodeBoolValues() => StringUtilities.EncodeValues(_boolArray);

        [Benchmark]
        public int[] SelectIntFromString_ToArray() => StringUtilities.SelectIntFromString(_csvInts).ToArray();

        [Benchmark]
        public double[] SelectDoubleFromString_ToArray() => StringUtilities.SelectDoubleFromString(_csvDoubles).ToArray();
    }
}
