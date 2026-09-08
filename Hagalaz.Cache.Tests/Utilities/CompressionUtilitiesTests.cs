using Hagalaz.Cache.Utilities;
using ICSharpCode.SharpZipLib.BZip2;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace Hagalaz.Cache.Tests.Utilities
{
    public class CompressionUtilitiesTests
    {
        [Fact]
        public void BzipDecompress_ShouldDecompressCorrectly()
        {
            // Arrange
            var originalString = "This is a test string for Bzip2 compression and decompression.";
            var originalData = Encoding.UTF8.GetBytes(originalString);

            // Act
            var compressedData = CompressionUtilities.BzipCompress(originalData);
            var decompressedData = CompressionUtilities.BzipDecompress(compressedData);
            var decompressedString = Encoding.UTF8.GetString(decompressedData);

            // Assert
            Assert.Equal(originalString, decompressedString);
        }

        [Fact]
        public void BzipDecompress_ShouldAcceptCachePayloadWithoutStreamHeader()
        {
            var originalData = Encoding.UTF8.GetBytes("Cache-style Bzip2 payload.");
            var cachePayload = CreateCachePayload(originalData);

            Assert.Equal("1AY&SY", Encoding.ASCII.GetString(cachePayload, 0, 6));

            var decompressedData = CompressionUtilities.BzipDecompress(cachePayload);

            Assert.Equal(originalData, decompressedData);
        }

        [Fact]
        public void BzipDecompress_ShouldAcceptLargeCachePayloadWithoutStreamHeader()
        {
            var originalData = new byte[512 * 1024];
            for (var i = 0; i < originalData.Length; i++)
                originalData[i] = (byte)((i * 31 + i / 17) % 256);

            var cachePayload = CreateCachePayload(originalData);

            var decompressedData = CompressionUtilities.BzipDecompress(cachePayload);

            Assert.Equal(originalData, decompressedData);
        }

        [Fact]
        public void BzipDecompress_ShouldRejectTruncatedHeaderlessPayload()
        {
            var truncatedPayload = Encoding.ASCII.GetBytes("1AY&");

            Assert.ThrowsAny<Exception>(() => CompressionUtilities.BzipDecompress(truncatedPayload));
        }

        private static byte[] CreateCachePayload(byte[] data)
        {
            using var stream = new MemoryStream();
            using (var compressor = new BZip2OutputStream(stream, 1))
            {
                compressor.Write(data, 0, data.Length);
            }

            var compressed = stream.ToArray();
            Assert.Equal("BZh1", Encoding.ASCII.GetString(compressed, 0, 4));
            return compressed[4..];
        }
    }
}
