using Hagalaz.Cache.Utilities;
using System.Text;


namespace Hagalaz.Cache.Tests.Utilities
{
    [TestClass]
    public class CompressionUtilitiesTests
    {
        [TestMethod]
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
    }
}
