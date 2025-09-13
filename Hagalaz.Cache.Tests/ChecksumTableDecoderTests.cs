using System.IO;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ChecksumTableDecoderTests
    {
        private static void WriteIntBigEndian(Stream stream, int value)
        {
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        [Fact]
        public void Decode_WithoutWhirlpool_ShouldReturnChecksumTable()
        {
            // Arrange
            var decoder = new ChecksumTableDecoder();
            using var stream = new MemoryStream();
            // Entry 1
            WriteIntBigEndian(stream, 123); // CRC
            WriteIntBigEndian(stream, 456); // Version
            // Entry 2
            WriteIntBigEndian(stream, 789); // CRC
            WriteIntBigEndian(stream, 101); // Version
            stream.Position = 0;

            // Act
            var table = decoder.Decode(stream);

            // Assert
            Assert.NotNull(table);
            Assert.Equal(2, table.Count);
            Assert.Equal(123, table.Entries[0].Crc32);
            Assert.Equal(456, table.Entries[0].Version);
            Assert.Equal(789, table.Entries[1].Crc32);
            Assert.Equal(101, table.Entries[1].Version);
        }

        [Fact]
        public void Decode_WithWhirlpool_ShouldReturnChecksumTable()
        {
            // Arrange
            var decoder = new ChecksumTableDecoder();
            using var stream = new MemoryStream();
            stream.WriteByte(1); // Number of entries
            // Entry 1
            WriteIntBigEndian(stream, 123); // CRC
            WriteIntBigEndian(stream, 456); // Version
            stream.Write(new byte[64]); // Whirlpool digest

            var dataToHash = stream.ToArray();
            var whirlpoolHash = Security.Whirlpool.GenerateDigest(dataToHash, 0, dataToHash.Length);

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
            Assert.Equal(123, table.Entries[0].Crc32);
            Assert.Equal(456, table.Entries[0].Version);
        }
    }
}
