using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Hagalaz.Cache.Extensions;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ChecksumTableCodecTests
    {
        private readonly IChecksumTableCodec _codec;

        public ChecksumTableCodecTests()
        {
            _codec = new ChecksumTableCodec();
        }

        private static void WriteIntBigEndian(Stream stream, int value)
        {
            stream.WriteByte((byte)(value >> 24));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)value);
        }

        private (RSAParameters, RSAParameters) GenerateRsaKeys()
        {
            using var rsa = new RSACryptoServiceProvider(1024);
            var privateKey = rsa.ExportParameters(true);
            var publicKey = rsa.ExportParameters(false);
            return (privateKey, publicKey);
        }

        [Fact]
        public void Decode_WithoutWhirlpool_ShouldReturnChecksumTable()
        {
            // Arrange
            using var stream = new MemoryStream();
            // Entry 1
            WriteIntBigEndian(stream, 123); // CRC
            WriteIntBigEndian(stream, 456); // Version
            // Entry 2
            WriteIntBigEndian(stream, 789); // CRC
            WriteIntBigEndian(stream, 101); // Version
            stream.Position = 0;

            // Act
            var table = _codec.Decode(stream);

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
            var table = _codec.Decode(finalStream, true);

            // Assert
            Assert.NotNull(table);
            Assert.Equal(1, table.Count);
            Assert.Equal(123, table.Entries[0].Crc32);
            Assert.Equal(456, table.Entries[0].Version);
        }

        [Fact]
        public void Encode_WithoutWhirlpool_ShouldReturnCorrectData()
        {
            // Arrange
            var table = new ChecksumTable(1);
            table.SetEntry(0, new ChecksumTableEntry(123, 456, new byte[64]));

            // Act
            var stream = _codec.Encode(table);

            // Assert
            Assert.NotNull(stream);
            Assert.Equal(8, stream.Length);
            stream.Position = 0;
            Assert.Equal(123, stream.ReadInt());
            Assert.Equal(456, stream.ReadInt());
        }

        [Fact]
        public void Encode_WithWhirlpool_ShouldReturnCorrectData()
        {
            // Arrange
            var table = new ChecksumTable(1);
            var digest = new byte[64];
            for (int i = 0; i < digest.Length; i++)
            {
                digest[i] = (byte)i;
            }
            table.SetEntry(0, new ChecksumTableEntry(123, 456, digest));

            // Act
            var stream = _codec.Encode(table, true);

            // Assert
            Assert.NotNull(stream);
            Assert.Equal(1 + 8 + 64 + 65, stream.Length); // 1 byte for entry count, 8 bytes for crc/version, 64 for digest, 65 for rsa block
            stream.Position = 0;
            Assert.Equal(1, stream.ReadByte()); // entry count
            Assert.Equal(123, stream.ReadInt());
            Assert.Equal(456, stream.ReadInt());
            var writtenDigest = new byte[64];
            stream.Read(writtenDigest, 0, 64);
            Assert.Equal(digest, writtenDigest);

            // Verify whirlpool hash
            stream.Position = 0;
            var dataToHash = new byte[1 + 8 + 64];
            stream.Read(dataToHash, 0, dataToHash.Length);
            var whirlpoolHash = Security.Whirlpool.GenerateDigest(dataToHash, 0, dataToHash.Length);

            Assert.Equal(10, stream.ReadByte()); // RSA block type
            var writtenWhirlpoolHash = new byte[64];
            stream.Read(writtenWhirlpoolHash, 0, 64);
            Assert.Equal(whirlpoolHash, writtenWhirlpoolHash);
        }

        [Fact]
        public void Encode_WithWhirlpoolAndRsa_ShouldReturnCorrectData()
        {
            // Arrange
            var (privateKey, publicKey) = GenerateRsaKeys();
            var table = new ChecksumTable(1);
            var digest = new byte[64];
            for (int i = 0; i < digest.Length; i++)
            {
                digest[i] = (byte)i;
            }
            table.SetEntry(0, new ChecksumTableEntry(123, 456, digest));

            // Act
            var stream = _codec.Encode(table, true, publicKey);

            var decodedTable = _codec.Decode(stream, true, privateKey);

            // Assert
            Assert.NotNull(decodedTable);
            Assert.Equal(1, decodedTable.Count);
            Assert.Equal(123, decodedTable.Entries[0].Crc32);
            Assert.Equal(456, decodedTable.Entries[0].Version);
            Assert.Equal(digest, decodedTable.Entries[0].Digest);
        }
    }
}
