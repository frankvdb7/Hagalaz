using System;
using System.IO;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Codecs;
using Xunit;

namespace Hagalaz.Cache.Tests;

public class RenderAnimationTypeCodecTests
{
    [Fact]
    public void Decode_CollectsStandWalkFirstThenOtherSequencesInOpcodeOrder()
    {
        var type = new RenderAnimationTypeCodec().Decode(0, new MemoryStream([26, 2, 3, 52, 2, 0, 12, 100, 0, 10, 50, 1, 0, 10, 0, 11, 0]));

        Assert.Equal(new[] { 10, 11, 12 }, type.SequenceIds);
        Assert.Equal(new byte[] { 2, 3 }, type.RawOpcodes[26]);
    }

    [Fact]
    public void Decode_KeepsTheLastValueOfARepeatedOpcode()
    {
        var type = new RenderAnimationTypeCodec().Decode(0, new MemoryStream([38, 0x35, 0xFC, 38, 0x35, 0xFD, 0]));

        Assert.Equal(0x35FD, type.Sequences[38]);
    }

    [Fact]
    public void Encode_WritesOpcodesInAscendingOrderAndIsStable()
    {
        var codec = new RenderAnimationTypeCodec();
        var type = codec.Decode(0, new MemoryStream([52, 1, 0, 12, 100, 28, 2, 7, 8, 2, 0, 5, 1, 0, 10, 0, 11, 53, 0]));

        using var first = codec.Encode(type);
        using var second = codec.Encode(codec.Decode(0, new MemoryStream(first.ToArray())));

        Assert.Equal(new byte[] { 1, 0, 10, 0, 11, 2, 0, 5, 28, 2, 7, 8, 52, 1, 0, 12, 100, 53, 0 }, first.ToArray());
        Assert.Equal(first.ToArray(), second.ToArray());
        Assert.Equal(new RenderAnimationSequencePair(10, 11), type.StandWalk);
    }

    [Fact]
    public void Decode_RejectsUnknownOpcodeAndTruncatedEntries()
    {
        var codec = new RenderAnimationTypeCodec();

        Assert.Throws<NotSupportedException>(() => codec.Decode(0, new MemoryStream([57, 0])));
        Assert.Throws<InvalidDataException>(() => codec.Decode(0, new MemoryStream([27, 1, 2])));
        Assert.Throws<InvalidDataException>(() => codec.Decode(0, new MemoryStream([2, 0, 5])));
    }
}
