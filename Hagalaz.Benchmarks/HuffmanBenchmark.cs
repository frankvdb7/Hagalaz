using System.IO;
using BenchmarkDotNet.Attributes;
using Hagalaz.Security;
using System.Linq;

namespace Hagalaz.Benchmarks
{
    [MemoryDiagnoser]
    public class HuffmanBenchmark
    {
        private byte[] _encodedData = null!;
        private int _messageLength;

        [Params(10, 100, 500)]
        public int Length;

        [GlobalSetup]
        public void Setup()
        {
            var text = new string(Enumerable.Repeat("abcdefghijklmnopqrstuvwxyz ", Length / 27 + 1).SelectMany(s => s).Take(Length).ToArray());
            _encodedData = Huffman.Encode(text, out _messageLength);
        }

        [Benchmark]
        public string Decode()
        {
            using var stream = new MemoryStream(_encodedData);
            return Huffman.Decode(stream, _messageLength);
        }
    }
}
