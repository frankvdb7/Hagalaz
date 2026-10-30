using System;
using System.IO;
using Hagalaz.Cache.Logic.Codecs;
using Xunit;

namespace Hagalaz.Cache.Tests;

public class MaterialDefinitionDecoderTests
{
    [Fact]
    public void Decode_ReadsColumnarModesWithAbsentMaterials()
    {
        // Three present materials, separated by an absent ID; all preceding columns are nonzero.
        var data = new byte[2 + 4 + 3 * 23];
        Array.Fill(data, (byte)255);
        data[0] = 0; data[1] = 4;
        data[2] = 1; data[3] = 0; data[4] = 1; data[5] = 1;
        data[^3] = 0; data[^2] = 1; data[^1] = 2;
        data[6 + 3 * 3 + 1] = 25; // Brightness column, second present material.
        data[6 + 3 * 4 + 1] = 170; // Color blend column, same material.
        Assert.Equal(0, MaterialDefinitionDecoder.Decode(data, 0).AlphaMode);
        Assert.Equal(1, MaterialDefinitionDecoder.Decode(data, 2).AlphaMode);
        Assert.Equal(2, MaterialDefinitionDecoder.Decode(data, 3).AlphaMode);
        Assert.Equal(25, MaterialDefinitionDecoder.Decode(data, 2).Brightness);
        Assert.Equal(170, MaterialDefinitionDecoder.Decode(data, 2).ColorBlend);
        Assert.Throws<NotSupportedException>(() => MaterialDefinitionDecoder.Decode(data, 1));
    }

    [Fact]
    public void Decode_RejectsTruncatedAlphaColumn()
    {
        Assert.Throws<InvalidDataException>(() => MaterialDefinitionDecoder.Decode(new byte[] { 0, 1, 1 }, 0));
    }
}
