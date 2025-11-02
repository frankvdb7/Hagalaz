using System.Numerics;
using System.Security.Cryptography;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Security;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class ChecksumTableCodecTests
    {
        [Fact]
        public void EncodeDecode_WithoutWhirlpool_ShouldBeEqual()
        {
            // Arrange
            var codec = new ChecksumTableCodec();
            var table = new ChecksumTable(2);
            table.SetEntry(0, new ChecksumTableEntry(123, 456, new byte[64]));
            table.SetEntry(1, new ChecksumTableEntry(789, 101, new byte[64]));

            // Act
            var encodedStream = codec.Encode(table);
            var decodedTable = codec.Decode(encodedStream);

            // Assert
            Assert.NotNull(decodedTable);
            Assert.Equal(table.Count, decodedTable.Count);
            for (int i = 0; i < table.Count; i++)
            {
                Assert.Equal(table[i].Crc32, decodedTable[i].Crc32);
                Assert.Equal(table[i].Version, decodedTable[i].Version);
            }
        }

        [Fact]
        public void EncodeDecode_WithWhirlpool_ShouldBeEqual()
        {
            // Arrange
            var codec = new ChecksumTableCodec();
            var table = new ChecksumTable(1);
            table.SetEntry(0, new ChecksumTableEntry(123, 456, new byte[64]));

            // Act
            var encodedStream = codec.Encode(table, true);
            var decodedTable = codec.Decode(encodedStream, true);

            // Assert
            Assert.NotNull(decodedTable);
            Assert.Equal(table.Count, decodedTable.Count);
            Assert.Equal(table[0].Crc32, decodedTable[0].Crc32);
            Assert.Equal(table[0].Version, decodedTable[0].Version);
        }

        [Fact]
        public void EncodeDecode_WithWhirlpoolAndRsa_ShouldBeEqual()
        {
            // Arrange
            var codec = new ChecksumTableCodec();
            var table = new ChecksumTable(1);
            table.SetEntry(0, new ChecksumTableEntry(123, 456, new byte[64]));

            using var rsa = RSA.Create(2048);
            var parameters = rsa.ExportParameters(true);
            var modulus = new BigInteger(parameters.Modulus, true, true);
            var privateKey = new BigInteger(parameters.D, true, true);
            var publicKey = new BigInteger(parameters.Exponent, true, true);

            // Act
            var encodedStream = codec.Encode(table, true, modulus, privateKey);
            var decodedTable = codec.Decode(encodedStream, true, modulus, publicKey);

            // Assert
            Assert.NotNull(decodedTable);
            Assert.Equal(table.Count, decodedTable.Count);
            Assert.Equal(table[0].Crc32, decodedTable[0].Crc32);
            Assert.Equal(table[0].Version, decodedTable[0].Version);
        }

        [Fact]
        public void Decode_WithoutWhirlpool_ShouldReturnChecksumTable()
        {
            // Arrange
            var codec = new ChecksumTableCodec();
            using var stream = new MemoryStream();
            // Entry 1
            stream.Write(System.BitConverter.GetBytes(123)); // CRC
            stream.Write(System.BitConverter.GetBytes(456)); // Version
            // Entry 2
            stream.Write(System.BitConverter.GetBytes(789)); // CRC
            stream.Write(System.BitConverter.GetBytes(101)); // Version
            stream.Position = 0;

            // Act
            var table = codec.Decode(stream);

            // Assert
            Assert.NotNull(table);
            Assert.Equal(2, table.Count);
        }

        [Fact]
        public void Decode_WithWhirlpool_ShouldReturnChecksumTable()
        {
            // Arrange
            var codec = new ChecksumTableCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(1); // Number of entries
            // Entry 1
            stream.Write(System.BitConverter.GetBytes(123)); // CRC
            stream.Write(System.BitConverter.GetBytes(456)); // Version
            stream.Write(new byte[64]); // Whirlpool digest

            var dataToHash = stream.ToArray();
            var whirlpoolHash = Whirlpool.GenerateDigest(dataToHash, 0, dataToHash.Length);

            using var finalStream = new MemoryStream();
            finalStream.Write(dataToHash);
            finalStream.WriteByte(10); // RSA block type, not important for this test
            finalStream.Write(whirlpoolHash);
            finalStream.Position = 0;

            // Act
            var table = codec.Decode(finalStream, true);

            // Assert
            Assert.NotNull(table);
            Assert.Equal(1, table.Count);
        }

        [Fact]
        public void EncodeDecode_EmptyTable_ShouldSucceed()
        {
            // Arrange
            var codec = new ChecksumTableCodec();
            var table = new ChecksumTable(0);

            // Act
            var encodedStream = codec.Encode(table, true);
            var decodedTable = codec.Decode(encodedStream, true);

            // Assert
            Assert.NotNull(decodedTable);
            Assert.Equal(0, decodedTable.Count);
        }

        [Fact]
        public void Decode_MismatchedWhirlpool_ShouldThrowIOException()
        {
            // Arrange
            var codec = new ChecksumTableCodec();
            using var stream = new MemoryStream();
            stream.WriteByte(1); // Number of entries
            // Entry 1
            stream.Write(System.BitConverter.GetBytes(123)); // CRC
            stream.Write(System.BitConverter.GetBytes(456)); // Version
            stream.Write(new byte[64]); // Whirlpool digest

            var dataToHash = stream.ToArray();
            var whirlpoolHash = Whirlpool.GenerateDigest(dataToHash, 0, dataToHash.Length);
            whirlpoolHash[0]++; // Corrupt the hash

            using var finalStream = new MemoryStream();
            finalStream.Write(dataToHash);
            finalStream.WriteByte(10); // RSA block type
            finalStream.Write(whirlpoolHash);
            finalStream.Position = 0;

            // Act & Assert
            Assert.Throws<IOException>(() => codec.Decode(finalStream, true));
        }

        [Fact]
        public void EncodeDecode_WithWhirlpoolAnd4096BitRsa_ShouldBeEqual()
        {
            // Arrange
            var codec = new ChecksumTableCodec();
            var table = new ChecksumTable(1);
            table.SetEntry(0, new ChecksumTableEntry(123, 456, new byte[64]));

            using var rsa = RSA.Create(4096);
            var parameters = rsa.ExportParameters(true);
            var modulus = new BigInteger(parameters.Modulus, true, true);
            var privateKey = new BigInteger(parameters.D, true, true);
            var publicKey = new BigInteger(parameters.Exponent, true, true);

            // Act
            var encodedStream = codec.Encode(table, true, modulus, privateKey);
            var decodedTable = codec.Decode(encodedStream, true, modulus, publicKey);

            // Assert
            Assert.NotNull(decodedTable);
            Assert.Equal(table.Count, decodedTable.Count);
            Assert.Equal(table[0].Crc32, decodedTable[0].Crc32);
            Assert.Equal(table[0].Version, decodedTable[0].Version);
        }
    }
}
