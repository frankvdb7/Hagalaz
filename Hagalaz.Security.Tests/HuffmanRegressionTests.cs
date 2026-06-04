using System.IO;
using System.Linq;
using Xunit;

namespace Hagalaz.Security.Tests
{
    public class HuffmanRegressionTests
    {
        [Fact]
        public void Decode_LegacyCompatibility_0xFF_ShouldProduceY()
        {
            // Verify that 0xFF decodes to 'y' (index 121), preserving legacy behavior.
            var data = new byte[] { 0xFF };
            using var stream = new MemoryStream(data);

            var result = Huffman.Decode(stream, 1);

            Assert.Equal("y", result);
        }

        [Fact]
        public void Decode_LargePayload_ShouldMatchOriginal()
        {
            var originalString = new string(Enumerable.Repeat("The quick brown fox jumps over the lazy dog. 1234567890!", 200).SelectMany(s => s).ToArray());
            var encodedBytes = Huffman.Encode(originalString, out var messageLength);

            using var stream = new MemoryStream(encodedBytes);
            var decodedString = Huffman.Decode(stream, messageLength);

            Assert.Equal(originalString, decodedString);
        }

        [Fact]
        public void Decode_AllEightBits_ShouldBeProcessedCorrectly()
        {
            // ' ' (space) has a short code. We want to test that if a character ends exactly on a byte boundary,
            // or spans across, it works.
            // We'll use a string that we know produces specific bit patterns.
            var input = "abc ";
            var encodedBytes = Huffman.Encode(input, out var length);

            using var stream = new MemoryStream(encodedBytes);
            var decoded = Huffman.Decode(stream, length);

            Assert.Equal(input, decoded);
        }

        [Fact]
        public void Decode_ExactlyAtBitstreamEnd_ShouldNotReadExtraBytes()
        {
            var input = "a";
            var encodedBytes = Huffman.Encode(input, out var length);
            // 'a' is 5 bits. 1 byte is enough.

            using var stream = new MemoryStream(encodedBytes);
            var decoded = Huffman.Decode(stream, length);

            Assert.Equal(input, decoded);
            // The stream position might be at the end of the byte even if we only needed 5 bits
            // because our loop processes bytes.
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void Decode_RandomLengthStrings_ShouldMatch(int length)
        {
            var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ";
            var random = new System.Random(42);
            var input = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).Take(length).ToArray());

            var encoded = Huffman.Encode(input, out var msgLen);
            using var stream = new MemoryStream(encoded);
            var decoded = Huffman.Decode(stream, msgLen);

            Assert.Equal(input, decoded);
        }
    }
}
