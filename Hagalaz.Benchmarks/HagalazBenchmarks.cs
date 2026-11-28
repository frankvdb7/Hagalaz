using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

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
            SetupCreatureBenchmarks();
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
