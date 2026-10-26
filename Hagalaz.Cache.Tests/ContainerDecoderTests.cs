using System.IO;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class ContainerDecoderTests
{
    [Theory]
    [InlineData(CompressionType.None, false)]
    [InlineData(CompressionType.Bzip2, true)]
    [InlineData(CompressionType.Gzip, true)]
    public void Decode_WhenDeclaredPayloadExceedsRemainingBytes_ThrowsInvalidDataException(CompressionType compressionType, bool compressed)
    {
        using var encoded = CreateHeader(compressionType, int.MaxValue, compressed);

        Assert.Throws<InvalidDataException>(() => new ContainerDecoder().Decode(encoded));
    }

    [Theory]
    [InlineData(CompressionType.None, false)]
    [InlineData(CompressionType.Bzip2, true)]
    [InlineData(CompressionType.Gzip, true)]
    public void Decode_WhenDeclaredPayloadLengthIsNegative_ThrowsInvalidDataException(CompressionType compressionType, bool compressed)
    {
        using var encoded = CreateHeader(compressionType, -1, compressed);

        Assert.Throws<InvalidDataException>(() => new ContainerDecoder().Decode(encoded));
    }

    private static MemoryStream CreateHeader(CompressionType compressionType, int compressedLength, bool compressed)
    {
        var encoded = new MemoryStream();
        encoded.WriteByte((byte)compressionType);
        encoded.WriteInt(compressedLength);
        if (compressed) encoded.WriteInt(0);
        encoded.Position = 0;
        return encoded;
    }
}
