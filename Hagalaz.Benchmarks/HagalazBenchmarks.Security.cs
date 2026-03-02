using BenchmarkDotNet.Attributes;
using Hagalaz.Security;

namespace Hagalaz.Benchmarks
{
    public partial class HagalazBenchmarks
    {
        private string _hashInput = "This is a test string for hashing performance benchmarks. It should be long enough to show some impact.";

        [Benchmark]
        public string ComputeHash_Benchmark() => HashHelper.ComputeHash(_hashInput, HashType.SHA256);
    }
}
