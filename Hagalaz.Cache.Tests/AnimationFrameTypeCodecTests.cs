using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Logic.Rendering;
using Xunit;
using System.Text.Json;

namespace Hagalaz.Cache.Tests;

public class AnimationFrameTypeCodecTests
{
    private static readonly AnimationSkeletonTypeCodec SkeletonCodec = new();
    private static readonly AnimationFrameTypeCodec FrameCodec = new();

    private static AnimationSkeleton Skeleton(byte[] data) => AnimationFrameBuilder.ResolveSkeleton(SkeletonCodec.Decode(0, new MemoryStream(data)));

    private static IAnimationFrameType Frame(byte[] data) => FrameCodec.Decode(0, new MemoryStream(data));

    [Fact]
    public void Decode_RealDemonSkeletonAndFramesMatchGameClient()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "black-demon-animation.json")));
        var root = fixture.RootElement;
        var skeletonBytes = Convert.FromBase64String(root.GetProperty("skeletonBytes").GetString()!);
        var skeleton = Skeleton(skeletonBytes);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var expectedSkeleton = root.GetProperty("skeleton").Deserialize<AnimationSkeleton>(options)!;
        Assert.Equal(expectedSkeleton.Types, skeleton.Types);
        Assert.Equal(expectedSkeleton.Enabled, skeleton.Enabled);
        Assert.Equal(expectedSkeleton.Masks, skeleton.Masks);
        Assert.Equal(expectedSkeleton.Groups, skeleton.Groups);
        using (var encoded = SkeletonCodec.Encode(SkeletonCodec.Decode(0, new MemoryStream(skeletonBytes)))) Assert.Equal(skeletonBytes, encoded.ToArray());
        foreach (var sample in root.GetProperty("frames").EnumerateArray())
        {
            var frameBytes = Convert.FromBase64String(sample.GetProperty("frameBytes").GetString()!);
            var expected = sample.GetProperty("frame").Deserialize<AnimationFrame>(options)!;
            var stored = Frame(frameBytes);
            var actual = AnimationFrameBuilder.Resolve(stored, skeleton);
            Assert.Equal(expected.SkeletonId, actual.SkeletonId);
            Assert.Equal(expected.Transforms, actual.Transforms);
            using var encoded = FrameCodec.Encode(stored);
            Assert.Equal(frameBytes, encoded.ToArray());
        }
    }

    [Fact]
    public void Resolve_PreservesTranslationAndImplicitPivot()
    {
        var skeleton = Skeleton([0, 2, 0, 1, 1, 1, 255, 255, 255, 255, 1, 1, 0, 2]);
        var frame = AnimationFrameBuilder.Resolve(Frame([0, 0, 7, 0, 2, 0, 7, 74, 54, 84]), skeleton);
        Assert.Equal(7, frame.SkeletonId);
        var transform = Assert.Single(frame.Transforms);
        Assert.Equal(new AnimationTransform(1, 10, -10, 20, 0, 0), transform);
        Assert.Equal(new[] { 2 }, skeleton.Groups[1]);
    }

    [Fact]
    public void Frame_RoundTripsTweenOnlyZeroAndTwoByteValuesWithFewerSlotsThanItsSkeleton()
    {
        // Slot 0: tween flags only. Slot 1: x stored as 0. Slot 2: y stored as 100 in the two-byte form.
        byte[] data = [1, 0, 3, 0, 3, 0x08, 0x01, 0x12, 64, 0xC0, 0x64];
        var frame = Frame(data);
        Assert.Equal(new AnimationSlotValues(null, null, null), frame.SlotValues[0]);
        Assert.Equal(new AnimationSlotValues(0, null, null), frame.SlotValues[1]);
        Assert.Equal(new AnimationSlotValues(null, 100, null), frame.SlotValues[2]);
        using var encoded = FrameCodec.Encode(frame);
        Assert.Equal(data, encoded.ToArray());

        var skeleton = Skeleton([0, 4, 3, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
        var resolved = AnimationFrameBuilder.Resolve(frame, skeleton);
        Assert.Equal(new AnimationTransform(0, 128, 128, 128, -1, 1), resolved.Transforms[0]);
        Assert.Equal(3, resolved.Transforms.Length);
    }

    [Fact]
    public void Resolve_RejectsAFrameWithMoreSlotsThanItsSkeleton()
    {
        var skeleton = Skeleton([0, 1, 0, 1, 0, 0, 0]);
        Assert.Throws<InvalidDataException>(() => AnimationFrameBuilder.Resolve(Frame([1, 0, 0, 0, 2, 0, 0]), skeleton));
    }

    [Fact]
    public void Decode_RejectsTruncatedAndTrailingData()
    {
        Assert.Throws<InvalidDataException>(() => SkeletonCodec.Decode(0, new MemoryStream([0, 2, 1])));
        Assert.Throws<InvalidDataException>(() => SkeletonCodec.Decode(0, new MemoryStream([0, 0, 9])));
        Assert.Throws<InvalidDataException>(() => Frame([1, 0, 0, 0, 1, 0x01]));
        Assert.Throws<InvalidDataException>(() => Frame([1, 0, 0, 0, 0, 9]));
    }

    [Fact]
    public void Skeleton_KeepsRawTypesAndEnabledBytesAndResolvesThem()
    {
        byte[] data = [0, 1, 6, 2, 0, 0, 0];
        var stored = SkeletonCodec.Decode(0, new MemoryStream(data));
        Assert.Equal(new[] { 6 }, stored.Types);
        Assert.Equal(new[] { 2 }, stored.Enabled);
        var resolved = AnimationFrameBuilder.ResolveSkeleton(stored);
        Assert.Equal(new[] { 2 }, resolved.Types);
        Assert.Equal(new[] { false }, resolved.Enabled);
        using var encoded = SkeletonCodec.Encode(stored);
        Assert.Equal(data, encoded.ToArray());
    }
}
