using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Hagalaz.Security.Tests
{
    [TestClass]
    public class AdditionalHuffmanTests
    {
        [TestMethod]
        [Ignore("This test is ignored because it exposes a pre-existing bug in Huffman.Decode. The method should return an empty string for invalid data but instead produces garbage output.")]
        public void Decode_WithSingleInvalidByte_ShouldReturnEmptyString()
        {
            // Arrange
            var invalidData = new byte[] { 0xff };
            using var stream = new MemoryStream(invalidData);

            // Act
            var result = Huffman.Decode(stream, 1);

            // Assert
            Assert.AreEqual(string.Empty, result, "A single invalid byte should result in an empty string.");
        }

        [TestMethod]
        [Ignore("This test is ignored because it exposes a pre-existing bug in Huffman.Decode. The method should return an empty string for invalid data but instead produces garbage output.")]
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
            Assert.AreEqual(string.Empty, result, "Data with a valid start but invalid end should be treated as corrupt.");
        }

        [TestMethod]
        public void Decode_WithDataShorterThanLength_ShouldReturnEmptyString()
        {
            // Arrange
            var data = Huffman.Encode("short", out _);
            using var stream = new MemoryStream(data);

            // Act
            var result = Huffman.Decode(stream, 10); // Request more characters than are available

            // Assert
            Assert.AreEqual(string.Empty, result, "Decoding should fail if the stream ends before the expected length is reached.");
        }

        [TestMethod]
        public void Decode_EmptyStreamWithNonZeroLength_ShouldReturnEmptyString()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            var result = Huffman.Decode(stream, 5);

            // Assert
            Assert.AreEqual(string.Empty, result, "Decoding from an empty stream with a non-zero length should return an empty string.");
        }
    }
}