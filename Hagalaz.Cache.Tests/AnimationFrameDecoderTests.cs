using Hagalaz.Cache.Logic.Codecs;
using Xunit;
using System.Text.Json;

namespace Hagalaz.Cache.Tests;

public class AnimationFrameDecoderTests
{
    [Fact]
    public void Decode_RealDemonSkeletonAndFramesMatchGameClient()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "black-demon-animation.json")));
        var root = fixture.RootElement;
        var skeleton = AnimationFrameDecoder.DecodeSkeleton(Convert.FromBase64String(root.GetProperty("skeletonBytes").GetString()!));
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var expectedSkeleton = root.GetProperty("skeleton").Deserialize<AnimationFrameDecoder.Skeleton>(options)!;
        Assert.Equal(expectedSkeleton.Types, skeleton.Types);
        Assert.Equal(expectedSkeleton.Enabled, skeleton.Enabled);
        Assert.Equal(expectedSkeleton.Masks, skeleton.Masks);
        Assert.Equal(expectedSkeleton.Groups, skeleton.Groups);
        foreach (var sample in root.GetProperty("frames").EnumerateArray())
        {
            var expected = sample.GetProperty("frame").Deserialize<AnimationFrameDecoder.Frame>(options)!;
            var actual = AnimationFrameDecoder.DecodeFrame(Convert.FromBase64String(sample.GetProperty("frameBytes").GetString()!), skeleton);
            Assert.Equal(expected.SkeletonId, actual.SkeletonId);
            Assert.Equal(expected.Transforms, actual.Transforms);
        }
    }

    [Fact]
    public void DecodeFrame_PreservesTranslationAndImplicitPivot()
    {
        var skeleton = AnimationFrameDecoder.DecodeSkeleton([0, 2, 0, 1, 1, 1, 255, 255, 255, 255, 1, 1, 0, 2]);
        var frame = AnimationFrameDecoder.DecodeFrame([0, 0, 7, 0, 2, 0, 7, 74, 54, 84], skeleton);
        Assert.Equal(7, frame.SkeletonId);
        var transform = Assert.Single(frame.Transforms);
        Assert.Equal(new AnimationFrameDecoder.Transform(1, 10, -10, 20, 0, 0), transform);
        Assert.Equal(new[] { 2 }, skeleton.Groups[1]);
    }

    [Fact]
    public void DecodeSkeleton_RejectsTruncatedData()
    {
        Assert.Throws<InvalidDataException>(() => AnimationFrameDecoder.DecodeSkeleton([0, 2, 1]));
    }
}
