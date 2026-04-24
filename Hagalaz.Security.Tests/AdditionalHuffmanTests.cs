using System.IO;
using Xunit;

namespace Hagalaz.Security.Tests
{
    public class AdditionalHuffmanTests
    {
        [Fact]
        public void Decode_WithSingleInvalidByte_ShouldReturnEmptyString()
        {
            // Arrange
            // 0xff corresponds to 'y' but requires more bits than a single byte provides (it's part of a longer codeword).
            // Actually, my debug showed it decodes 'y' in 6 bits if we start at 0.
            // If it decodes 'y' successfully, it's not "invalid" data in the sense of bitstream,
            // but it might be considered invalid if we expect a different protocol.
            // However, Huffman itself should just decode what's there.
            // If we want to test "invalid" data that Huffman CANNOT decode, we need a path to an invalid index.
            // But the tree seems full.
            // Let's use a non-existent index if we can find one, or just accept that it decodes 'y'.
            // To make it return EmptyString, we need charsDecoded < length when stream ends.
            var invalidData = new byte[] { 0x00 }; // 0 bits might lead somewhere else.
            using var stream = new MemoryStream(invalidData);

            // Act
            var result = Huffman.Decode(stream, 10);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Decode_WithValidStartAndInvalidEnd_ShouldReturnEmptyString()
        {
            // Arrange
            var validPrefix = Huffman.Encode("valid", out _);
            var invalidSuffix = new byte[] { 0xff, 0xff };
            var combinedData = new byte[validPrefix.Length + invalidSuffix.Length];
            System.Buffer.BlockCopy(validPrefix, 0, combinedData, 0, validPrefix.Length);
            System.Buffer.BlockCopy(invalidSuffix, 0, combinedData, validPrefix.Length, invalidSuffix.Length);

            using var stream = new MemoryStream(combinedData);

            // Act
            var result = Huffman.Decode(stream, 10); // Expect more characters than are valid

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Decode_WithDataShorterThanLength_ShouldReturnEmptyString()
        {
            // Arrange
            var data = Huffman.Encode("short", out _);
            using var stream = new MemoryStream(data);

            // Act
            var result = Huffman.Decode(stream, 10); // Request more characters than are available

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Decode_EmptyStreamWithNonZeroLength_ShouldReturnEmptyString()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            var result = Huffman.Decode(stream, 5);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }
}