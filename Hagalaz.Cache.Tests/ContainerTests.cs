using System.IO;
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
        var decoder = new ContainerDecoder();

        // Act
        var encoded = container.Encode();
        var decoded = decoder.Decode(new MemoryStream(encoded));

        // Assert
        Assert.Equal(data.ToArray(), decoded.Data.ToArray());
    }

    [Fact]
    public void Encode_GzipCompression_ReturnsCorrectData()
    {
        // Arrange
        var data = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var container = new Container(CompressionType.Gzip, data);
        var decoder = new ContainerDecoder();

        // Act
        var encoded = container.Encode();
        var decoded = decoder.Decode(new MemoryStream(encoded));

        // Assert
        Assert.Equal(data.ToArray(), decoded.Data.ToArray());
    }

    [Fact]
    public void Encode_Bzip2Compression_ReturnsCorrectData()
    {
        // Arrange
        var data = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var container = new Container(CompressionType.Bzip2, data);
        var decoder = new ContainerDecoder();

        // Act
        var encoded = container.Encode();
        var decoded = decoder.Decode(new MemoryStream(encoded));

        // Assert
        Assert.Equal(data.ToArray(), decoded.Data.ToArray());
    }
}
