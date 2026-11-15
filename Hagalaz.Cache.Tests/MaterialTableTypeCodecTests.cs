using System;
using System.IO;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests;

public class MaterialTableTypeCodecTests
{
    // Three present materials, separated by an absent ID; all preceding columns are nonzero.
    private static byte[] Table()
    {
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
        return data;
    }

    [Fact]
    public void Decode_ReadsColumnarModesWithAbsentMaterials()
    {
        var table = new MaterialTableTypeCodec().Decode(0, new MemoryStream(Table()));

        Assert.Equal(0, MaterialTableType.Require(table, 0).AlphaMode);
        Assert.Equal(1, MaterialTableType.Require(table, 2).AlphaMode);
        Assert.Equal(2, MaterialTableType.Require(table, 3).AlphaMode);
        var material = MaterialTableType.Require(table, 2);
        Assert.Equal(25, material.Brightness);
        Assert.Equal(170, material.ColorBlend);
        Assert.Equal(7, material.EffectId);
        Assert.Equal(33, material.EffectParameter);
        Assert.Equal(-1, material.ScrollU);
        Assert.Equal(2, material.ScrollV);
        Assert.True(material.RepeatU);
        Assert.False(material.RepeatV);
        Assert.True(material.HasHdr);
        Assert.Equal(unchecked((int)0x91abcdef), material.EffectParameter2);
        Assert.Null(table.Find(1));
        Assert.Throws<NotSupportedException>(() => MaterialTableType.Require(table, 1));
    }

    [Fact]
    public void Encode_ReproducesTheStoredTableByteForByte()
    {
        var codec = new MaterialTableTypeCodec();
        var stored = Table();

        using var encoded = codec.Encode(codec.Decode(0, new MemoryStream(stored)));

        Assert.Equal(stored, encoded.ToArray());
    }

    [Fact]
    public void Decode_RejectsTruncatedOrTrailingColumns()
    {
        var codec = new MaterialTableTypeCodec();

        Assert.Throws<InvalidDataException>(() => codec.Decode(0, new MemoryStream([0, 1, 1])));
        Assert.Throws<InvalidDataException>(() => codec.Decode(0, new MemoryStream([.. Table(), 0])));
    }
}
