using System;
using System.Collections.Generic;
using System.IO;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Revision-742 animation skeleton and frame decoding.</summary>
public static class AnimationFrameDecoder
{
    public sealed record Skeleton(int[] Types, bool[] Enabled, int[] Masks, int[][] Groups);
    public sealed record Transform(int Slot, int X, int Y, int Z, int PivotSlot, int TweenFlags);
    public sealed record Frame(int SkeletonId, Transform[] Transforms);

    public static Skeleton DecodeSkeleton(byte[] data)
    {
        var reader = new Reader(data);
        var count = reader.Short();
        var types = new int[count];
        var enabled = new bool[count];
        var masks = new int[count];
        var groups = new int[count][];
        for (var slot = 0; slot < count; slot++) { var type = reader.Byte(); types[slot] = type == 6 ? 2 : type; }
        for (var slot = 0; slot < count; slot++) enabled[slot] = reader.Byte() == 1;
        for (var slot = 0; slot < count; slot++) masks[slot] = reader.Short();
        for (var slot = 0; slot < count; slot++) groups[slot] = new int[reader.UnsignedSmart()];
        foreach (var group in groups)
            for (var entry = 0; entry < group.Length; entry++) group[entry] = reader.UnsignedSmart();
        if (reader.Position != data.Length) throw new InvalidDataException("Unexpected skeleton trailing data.");
        return new Skeleton(types, enabled, masks, groups);
    }

    public static Frame DecodeFrame(byte[] data, Skeleton skeleton)
    {
        var flags = new Reader(data);
        flags.Byte();
        var skeletonId = flags.Short();
        var count = flags.Short();
        if (count > skeleton.Types.Length) throw new InvalidDataException("Frame exceeds skeleton transform count.");
        var values = new Reader(data) { Position = flags.Position + count };
        var transforms = new List<Transform>();
        int lastPivot = -1, usedPivot = -1;
        for (var slot = 0; slot < count; slot++)
        {
            var type = skeleton.Types[slot];
            if (type == 0) lastPivot = slot;
            var bits = flags.Byte();
            if (bits == 0) continue;
            if (type == 0) usedPivot = slot;
            var defaultValue = type is 3 or 10 ? 128 : 0;
            var x = (bits & 1) != 0 ? values.SignedSmart() : defaultValue;
            var y = (bits & 2) != 0 ? values.SignedSmart() : defaultValue;
            var z = (bits & 4) != 0 ? values.SignedSmart() : defaultValue;
            if (type is 2 or 9) { x = (x << 2) & 16383; y = (y << 2) & 16383; z = (z << 2) & 16383; }
            var pivot = -1;
            if (type is 1 or 2 or 3 && lastPivot > usedPivot) { pivot = lastPivot; usedPivot = lastPivot; }
            transforms.Add(new Transform(slot, x, y, z, pivot, (bits >> 3) & 3));
        }
        if (values.Position != data.Length) throw new InvalidDataException("Unexpected animation frame trailing data.");
        return new Frame(skeletonId, transforms.ToArray());
    }

    private sealed class Reader(byte[] data)
    {
        public int Position { get; set; }
        public int Byte()
        {
            if ((uint)Position >= data.Length) throw new InvalidDataException("Animation data is truncated.");
            return data[Position++];
        }
        public int Short() => (Byte() << 8) | Byte();
        public int SignedSmart() => Peek() < 128 ? Byte() - 64 : Short() - 49152;
        public int UnsignedSmart() => Peek() < 128 ? Byte() : Short() - 32768;
        private int Peek()
        {
            if ((uint)Position >= data.Length) throw new InvalidDataException("Animation data is truncated.");
            return data[Position];
        }
    }
}
