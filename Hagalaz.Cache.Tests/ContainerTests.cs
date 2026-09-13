using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Utilities;
using ICSharpCode.SharpZipLib.BZip2;
using System.IO;
using System.Text;
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

    [Fact]
    public void Decode_Bzip2CachePayloadWithoutStreamHeader_PreservesDataAndVersion()
    {
        // Cache containers store the BZip2 block without the standalone BZh1 prefix.
        var originalData = Encoding.UTF8.GetBytes("Cache container payload.");
        var cachePayload = CreateCachePayload(originalData);
        using var encoded = new MemoryStream();
        encoded.WriteByte((byte)CompressionType.Bzip2);
        encoded.WriteInt(cachePayload.Length);
        encoded.WriteInt(originalData.Length);
        encoded.Write(cachePayload, 0, cachePayload.Length);
        encoded.WriteShort(1234);
        encoded.Position = 0;

        var decoded = new ContainerDecoder().Decode(encoded);

        Assert.Equal(CompressionType.Bzip2, decoded.CompressionType);
        Assert.Equal((short)1234, decoded.Version);
        Assert.Equal(originalData, decoded.Data.ToArray());
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
