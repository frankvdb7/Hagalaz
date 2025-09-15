using System.IO;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ChecksumTableDecoderTests
    {
        [Fact]
        public void Decode_WithoutWhirlpool_ShouldReturnChecksumTable()
        {
            // Arrange
            var decoder = new ChecksumTableDecoder();
            using var stream = new MemoryStream();
            // Entry 1
            stream.Write(System.BitConverter.GetBytes(123)); // CRC
            stream.Write(System.BitConverter.GetBytes(456)); // Version
            // Entry 2
            stream.Write(System.BitConverter.GetBytes(789)); // CRC
            stream.Write(System.BitConverter.GetBytes(101)); // Version
            stream.Position = 0;

            // Act
            var table = decoder.Decode(stream);

            // Assert
            Assert.NotNull(table);
            Assert.Equal(2, table.Count);
        }

        [Fact]
        public void Decode_WithWhirlpool_ShouldReturnChecksumTable()
        {
            // Arrange
            var decoder = new ChecksumTableDecoder();
            using var stream = new MemoryStream();
            stream.WriteByte(1); // Number of entries
            // Entry 1
            stream.Write(System.BitConverter.GetBytes(123)); // CRC
            stream.Write(System.BitConverter.GetBytes(456)); // Version
            stream.Write(new byte[64]); // Whirlpool digest

            var dataToHash = stream.ToArray();
            var whirlpoolHash = Hagalaz.Security.Whirlpool.GenerateDigest(dataToHash, 0, dataToHash.Length);

            using var finalStream = new MemoryStream();
            finalStream.Write(dataToHash);
            finalStream.WriteByte(10); // RSA block type, not important for this test
            finalStream.Write(whirlpoolHash);
            finalStream.Position = 0;

            // Act
            var table = decoder.Decode(finalStream, true);

            // Assert
            Assert.NotNull(table);
            Assert.Equal(1, table.Count);
        }
    }
}
