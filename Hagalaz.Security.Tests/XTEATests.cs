using System;
using Xunit;

namespace Hagalaz.Security.Tests
{
    public class XTEATests
    {
        [Fact]
        public void Encrypt_Then_Decrypt_ShouldReturnOriginalData()
        {
            // Arrange
            var key = new uint[] { 0x12345678, 0x9ABCDEF0, 0x11223344, 0x55667788 };
            var originalData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var encryptedData = new byte[originalData.Length];
            var decryptedData = new byte[originalData.Length];

            // Act
            XTEA.Encrypt(originalData, encryptedData, key);
            XTEA.Decrypt(encryptedData, decryptedData, key);

            // Assert
            Assert.Equal(originalData, decryptedData);
        }

        [Fact]
        public void Encrypt_Then_Decrypt_WithMultipleBlocks_ShouldReturnOriginalData()
        {
            // Arrange
            var key = new uint[] { 0x12345678, 0x9ABCDEF0, 0x11223344, 0x55667788 };
            var originalData = new byte[]
            {
                1, 2, 3, 4, 5, 6, 7, 8,
                9, 10, 11, 12, 13, 14, 15, 16,
                17, 18, 19, 20, 21, 22, 23, 24
            };
            var encryptedData = new byte[originalData.Length];
            var decryptedData = new byte[originalData.Length];

            // Act
            XTEA.Encrypt(originalData, encryptedData, key);
            XTEA.Decrypt(encryptedData, decryptedData, key);

            // Assert
            Assert.Equal(originalData, decryptedData);
        }

        [Fact]
        public void Encrypt_WithOffset_ShouldProcessCorrectSegment()
        {
            // Arrange
            var key = new uint[] { 0x1, 0x2, 0x3, 0x4 };
            var originalData = new byte[] { 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8 }; // Prefix garbage
            var encryptedData = new byte[originalData.Length];
            var decryptedData = new byte[originalData.Length];
            var offset = 4;

            var expectedEncryptedPrefix = new byte[] { 0, 0, 0, 0 };

            // Act
            XTEA.Encrypt(originalData, encryptedData, key, start: offset);
            XTEA.Decrypt(encryptedData, decryptedData, key, start: offset);

            // Assert
            // Check that the prefix was not touched
            for(int i = 0; i < offset; i++)
            {
                Assert.Equal(expectedEncryptedPrefix[i], encryptedData[i]);
            }

            // Check that the encrypted part is not the same as the original
            Assert.NotEqual(
                new Span<byte>(originalData).Slice(offset).ToArray(),
                new Span<byte>(encryptedData).Slice(offset).ToArray()
            );

            // Check that the decrypted data matches the original segment
            Assert.Equal(
                new Span<byte>(originalData).Slice(offset).ToArray(),
                new Span<byte>(decryptedData).Slice(offset).ToArray()
            );
        }

        [Fact]
        public void Encrypt_InputNotMultipleOfBlockSize_ShouldIgnoreTrailingBytes()
        {
            // Arrange
            var key = new uint[] { 0x1, 0x2, 0x3, 0x4 };
            var originalData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }; // 11 bytes
            var encryptedData = new byte[originalData.Length];
            originalData.CopyTo(encryptedData, 0); // Pre-fill the output buffer

            var originalBlock = new Span<byte>(originalData, 0, 8).ToArray();
            var encryptedBlock = new byte[8];

            // Act
            XTEA.Encrypt(originalData, encryptedData, key);
            XTEA.EncryptBlock(originalBlock, encryptedBlock, key, 0, 32);

            // Assert
            // Check that the first block was encrypted correctly
            Assert.Equal(encryptedBlock, new Span<byte>(encryptedData, 0, 8).ToArray());

            // Check that trailing bytes were not modified
            for (int i = 8; i < originalData.Length; i++)
            {
                Assert.Equal(originalData[i], encryptedData[i]);
            }
        }
    }
}