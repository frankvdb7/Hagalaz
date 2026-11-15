using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Logic.Rendering;

public sealed record AnimationSkeleton(int[] Types, bool[] Enabled, int[] Masks, int[][] Groups);

public sealed record AnimationTransform(int Slot, int X, int Y, int Z, int PivotSlot, int TweenFlags);

public sealed record AnimationFrame(int SkeletonId, AnimationTransform[] Transforms);

/// <summary>Resolves stored skeletons and frames into the transforms the revision-742 client applies.</summary>
public static class AnimationFrameBuilder
{
    public static AnimationSkeleton ResolveSkeleton(IAnimationSkeletonType skeleton) => new(
        skeleton.Types.Select(type => type == 6 ? 2 : type).ToArray(),
        skeleton.Enabled.Select(enabled => enabled == 1).ToArray(),
        skeleton.Masks,
        skeleton.Groups);

    public static AnimationFrame Resolve(IAnimationFrameType frame, AnimationSkeleton skeleton)
    {
        if (frame.SlotFlags.Length > skeleton.Types.Length) throw new InvalidDataException("Frame exceeds skeleton transform count.");
        var transforms = new List<AnimationTransform>();
        int lastPivot = -1, usedPivot = -1;
        for (var slot = 0; slot < frame.SlotFlags.Length; slot++)
        {
            var type = skeleton.Types[slot];
            if (type == 0) lastPivot = slot;
            var bits = frame.SlotFlags[slot];
            if (bits == 0) continue;
            if (type == 0) usedPivot = slot;
            var defaultValue = type is 3 or 10 ? 128 : 0;
            var values = frame.SlotValues[slot];
            var (x, y, z) = (values.X ?? defaultValue, values.Y ?? defaultValue, values.Z ?? defaultValue);
            if (type is 2 or 9) { x = (x << 2) & 16383; y = (y << 2) & 16383; z = (z << 2) & 16383; }
            var pivot = -1;
            if (type is 1 or 2 or 3 && lastPivot > usedPivot) { pivot = lastPivot; usedPivot = lastPivot; }
            transforms.Add(new AnimationTransform(slot, x, y, z, pivot, (bits >> 3) & 3));
        }
        return new AnimationFrame(frame.SkeletonId, transforms.ToArray());
    }
}
