using System.IO;
using Hagalaz.Cache.Utilities;
using Xunit;

namespace Hagalaz.Cache.Tests;

public class ContainerTests
{
    [Fact]
    public void Encode_NoneCompression_ReturnsCorrectData()
    {
        // Arrange
        var data = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var container = new Container(CompressionType.None, data);

        // Act
        var encoded = container.Encode();

        // Assert
        var expected = new byte[] { 0, 0, 0, 0, 5, 1, 2, 3, 4, 5 };
        Assert.Equal(expected, encoded);
    }

    [Fact]
    public void Encode_GzipCompression_ReturnsCorrectData()
    {
        // Arrange
        var data = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var container = new Container(CompressionType.Gzip, data);

        // Act
        var encoded = container.Encode();

        // Assert
        var compressed = CompressionUtilities.GzipCompress(data.ToArray());
        var expected = new byte[9 + compressed.Length];
        expected[0] = (byte)CompressionType.Gzip;
        expected[1] = (byte)(compressed.Length >> 24);
        expected[2] = (byte)(compressed.Length >> 16);
        expected[3] = (byte)(compressed.Length >> 8);
        expected[4] = (byte)compressed.Length;
        expected[5] = (byte)(data.Length >> 24);
        expected[6] = (byte)(data.Length >> 16);
        expected[7] = (byte)(data.Length >> 8);
        expected[8] = (byte)data.Length;
        System.Array.Copy(compressed, 0, expected, 9, compressed.Length);

        Assert.Equal(expected, encoded);
    }

    [Fact]
    public void Encode_Bzip2Compression_ReturnsCorrectData()
    {
        // Arrange
        var data = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var container = new Container(CompressionType.Bzip2, data);

        // Act
        var encoded = container.Encode();

        // Assert
        var compressed = CompressionUtilities.BzipCompress(data.ToArray());
        var expected = new byte[9 + compressed.Length];
        expected[0] = (byte)CompressionType.Bzip2;
        expected[1] = (byte)(compressed.Length >> 24);
        expected[2] = (byte)(compressed.Length >> 16);
        expected[3] = (byte)(compressed.Length >> 8);
        expected[4] = (byte)compressed.Length;
        expected[5] = (byte)(data.Length >> 24);
        expected[6] = (byte)(data.Length >> 16);
        expected[7] = (byte)(data.Length >> 8);
        expected[8] = (byte)data.Length;
        System.Array.Copy(compressed, 0, expected, 9, compressed.Length);

        Assert.Equal(expected, encoded);
    }
}
