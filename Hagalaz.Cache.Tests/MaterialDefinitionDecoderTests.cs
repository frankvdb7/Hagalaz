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
        data[6 + 3 * 5 + 1] = 7;
        data[6 + 3 * 6 + 1] = 33;
        data[6 + 3 * 9 + 1] = 255;
        data[6 + 3 * 10 + 1] = 2;
        data[6 + 3 * 14 + 1] = 1;
        data[6 + 3 * 15 + 1] = 0;
        data[6 + 3 * 16 + 1] = 1;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(6 + 3 * 18 + 4, 4), unchecked((int)0x91abcdef));
        Assert.Equal(0, MaterialDefinitionDecoder.Decode(data, 0).AlphaMode);
        Assert.Equal(1, MaterialDefinitionDecoder.Decode(data, 2).AlphaMode);
        Assert.Equal(2, MaterialDefinitionDecoder.Decode(data, 3).AlphaMode);
        Assert.Equal(25, MaterialDefinitionDecoder.Decode(data, 2).Brightness);
        Assert.Equal(170, MaterialDefinitionDecoder.Decode(data, 2).ColorBlend);
        var material = MaterialDefinitionDecoder.Decode(data, 2);
        Assert.Equal(7, material.EffectId);
        Assert.Equal(33, material.EffectParameter);
        Assert.Equal(-1, material.ScrollU);
        Assert.Equal(2, material.ScrollV);
        Assert.True(material.RepeatU);
        Assert.False(material.RepeatV);
        Assert.True(material.HasHdr);
        Assert.Equal(unchecked((int)0x91abcdef), material.EffectParameter2);
        Assert.Throws<NotSupportedException>(() => MaterialDefinitionDecoder.Decode(data, 1));
    }

    [Fact]
    public void Decode_RejectsTruncatedAlphaColumn()
    {
        Assert.Throws<InvalidDataException>(() => MaterialDefinitionDecoder.Decode(new byte[] { 0, 1, 1 }, 0));
    }
}
