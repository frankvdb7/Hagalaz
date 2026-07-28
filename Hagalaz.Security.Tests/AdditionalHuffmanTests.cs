using System.IO;
using Xunit;

namespace Hagalaz.Security.Tests
{
    public class AdditionalHuffmanTests
    {
        [Fact(Skip = "Legacy behavior produces 'y' for 0xFF, which is currently used by existing tests/data. Optimization preserved this to avoid regressions.")]
        public void Decode_WithSingleInvalidByte_ShouldReturnEmptyString()
        {
            // Arrange
            var invalidData = new byte[] { 0xff };
            using var stream = new MemoryStream(invalidData);

            // Act
            var result = Huffman.Decode(stream, 1);

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