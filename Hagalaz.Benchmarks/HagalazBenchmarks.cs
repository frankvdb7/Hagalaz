using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;
using Hagalaz.Collections;
using Hagalaz.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    [MemoryDiagnoser]
    [JsonExporterAttribute.Full]
    public class HagalazBenchmarks
    {
        private List<int> _list = null!;
        private ListHashSet<int> _listHashSet = null!;
        private int _lookupValue;

        private List<int> _visibleCreaturesList = null!;
        private ListHashSet<int> _visibleCreaturesListHashSet = null!;
        private List<int> _localEntities = null!;

        private string _csvInts = string.Empty;
        private string _csvBools = string.Empty;
        private int[] _intArray = null!;
        private bool[] _boolArray = null!;

        [Params(100, 1000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            // Collection Setup
            _list = Enumerable.Range(0, N).ToList();
            _listHashSet = _list.ToListHashSet();
            _lookupValue = N - 1;

            // Viewport Simulation Setup
            _visibleCreaturesList = Enumerable.Range(0, N).ToList();
            _visibleCreaturesListHashSet = _visibleCreaturesList.ToListHashSet();
            _localEntities = Enumerable.Range(N / 2, 255).ToList();

            // String Parsing Setup
            _csvInts = string.Join(",", Enumerable.Range(0, N));
            _csvBools = string.Join(",", Enumerable.Range(0, N).Select(i => i % 2 == 0 ? "1" : "0"));
            _intArray = Enumerable.Range(0, N).ToArray();
            _boolArray = Enumerable.Range(0, N).Select(i => i % 2 == 0).ToArray();
        }

        [Benchmark]
        public bool ListContains() => _list.Contains(_lookupValue);

        [Benchmark]
        public bool ListHashSetContains() => _listHashSet.Contains(_lookupValue);

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

        [Benchmark]
        public ListHashSet<int> ToListHashSet_Benchmark() => _list.ToListHashSet();
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
