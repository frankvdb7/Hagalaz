using BenchmarkDotNet.Running;
using Hagalaz.Benchmarks.StringParsing;

namespace Hagalaz.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
