using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Hagalaz.Security.Tests
{
    [TestClass]
    public class XTEATests
    {
        [TestMethod]
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
            CollectionAssert.AreEqual(originalData, decryptedData, "Decrypted data does not match the original data.");
        }

        [TestMethod]
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
            CollectionAssert.AreEqual(originalData, decryptedData, "Decrypted data for multiple blocks does not match the original data.");
        }

        [TestMethod]
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
                Assert.AreEqual(expectedEncryptedPrefix[i], encryptedData[i], $"Encrypted data prefix at index {i} was modified unexpectedly.");
            }

            // Check that the encrypted part is not the same as the original
            CollectionAssert.AreNotEqual(
                new Span<byte>(originalData).Slice(offset).ToArray(),
                new Span<byte>(encryptedData).Slice(offset).ToArray(),
                "The encrypted segment should not be the same as the original."
            );

            // Check that the decrypted data matches the original segment
            CollectionAssert.AreEqual(
                new Span<byte>(originalData).Slice(offset).ToArray(),
                new Span<byte>(decryptedData).Slice(offset).ToArray(),
                "Decrypted data does not match the original data segment."
            );
        }

        [TestMethod]
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
            CollectionAssert.AreEqual(encryptedBlock, new Span<byte>(encryptedData, 0, 8).ToArray(), "The first block was not encrypted correctly.");

            // Check that trailing bytes were not modified
            for (int i = 8; i < originalData.Length; i++)
            {
                Assert.AreEqual(originalData[i], encryptedData[i], $"Trailing byte at index {i} was modified.");
            }
        }

        [TestMethod]
        public void TestTransform()
        {
            var key = new uint[] { 1, 2, 3, 4 };

            var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var originalData = (byte[])data.Clone();

            var encryptedData = new byte[data.Length];
            XTEA.Encrypt(data, encryptedData, key);

            Assert.AreNotEqual(originalData, encryptedData);

            var decryptedData = new byte[data.Length];
            XTEA.Decrypt(encryptedData, decryptedData, key);

            CollectionAssert.AreEqual(originalData, decryptedData);
        }
    }
}