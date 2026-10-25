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
    [InlineData(CompressionType.Gzip, true)]
    public void Decode_WhenDeclaredPayloadIsTruncated_ThrowsEndOfStreamException(CompressionType compressionType, bool compressed)
    {
        using var encoded = new MemoryStream();
        encoded.WriteByte((byte)compressionType);
        encoded.WriteInt(3);
        if (compressed) encoded.WriteInt(3);
        encoded.Position = 0;

        Assert.Throws<EndOfStreamException>(() => new ContainerDecoder().Decode(encoded));
    }
}
