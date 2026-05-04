using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;
using Hagalaz.Collections;
using Hagalaz.Utilities;
using Hagalaz.Security;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    [MemoryDiagnoser]
    [JsonExporterAttribute.Full]
    public partial class HagalazBenchmarks
    {
        [Params(100, 1000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            SetupCollections();
            SetupViewport();
            SetupStringParsing();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
