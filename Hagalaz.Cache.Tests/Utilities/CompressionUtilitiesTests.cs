using Hagalaz.Cache.Utilities;
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
            var strippedData = new byte[compressedData.Length - 4];
            System.Array.Copy(compressedData, 4, strippedData, 0, strippedData.Length);
            var decompressedData = CompressionUtilities.BzipDecompress(strippedData);
            var decompressedString = Encoding.UTF8.GetString(decompressedData);

            // Assert
            Assert.Equal(originalString, decompressedString);
        }
    }
}
