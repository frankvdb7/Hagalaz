using Hagalaz.Cache.Logic.Codecs;
using Xunit;

namespace Hagalaz.Cache.Tests;

public class RenderAnimationSetDecoderTests
{
    [Fact]
    public void Decode_CollectsStandingWalkingAndWeightedLoopSequences()
    {
        using var stream = new MemoryStream([1, 0, 10, 0, 11, 26, 2, 3, 52, 2, 0, 12, 100, 0, 10, 50, 0]);
        Assert.Equal(new[] { 10, 11, 12 }, RenderAnimationSetDecoder.Decode(stream));
    }

    [Fact]
    public void Decode_RejectsUnknownOpcodeRatherThanMisreadingPayload()
    {
        using var stream = new MemoryStream([57, 0]);
        Assert.Throws<NotSupportedException>(() => RenderAnimationSetDecoder.Decode(stream));
    }
}
