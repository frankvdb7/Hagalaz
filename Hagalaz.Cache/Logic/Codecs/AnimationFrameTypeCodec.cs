using System;
using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>
/// Revision-742 animation frames, cache index 0: a header byte, the skeleton ID, the slot count, one flag byte per slot, then a
/// signed smart for each set axis bit. Needs no skeleton; flag bytes are kept as stored.
/// </summary>
public sealed class AnimationFrameTypeCodec : ITypeCodec<IAnimationFrameType>
{
    public IAnimationFrameType Decode(int id, MemoryStream stream)
    {
        var reader = new DefinitionReader(stream, "animation frame");
        var frame = new AnimationFrameType(id) { Header = reader.Byte(), SkeletonId = reader.UShort() };
        var count = reader.UShort();
        frame.SlotFlags = new int[count];
        frame.SlotValues = new AnimationSlotValues[count];
        for (var slot = 0; slot < count; slot++) frame.SlotFlags[slot] = reader.Byte();
        for (var slot = 0; slot < count; slot++)
        {
            var flags = frame.SlotFlags[slot];
            frame.SlotValues[slot] = new(
                (flags & 1) != 0 ? reader.SignedSmart() : null,
                (flags & 2) != 0 ? reader.SignedSmart() : null,
                (flags & 4) != 0 ? reader.SignedSmart() : null);
        }
        reader.End();
        return frame;
    }

    public MemoryStream Encode(IAnimationFrameType frame)
    {
        if (frame.SlotValues.Length != frame.SlotFlags.Length) throw new InvalidOperationException("Every frame slot needs a flag byte and its values.");
        var stream = new MemoryStream();
        stream.Byte(frame.Header);
        stream.UShort(frame.SkeletonId);
        stream.UShort(frame.SlotFlags.Length);
        foreach (var flags in frame.SlotFlags) stream.Byte(flags);
        for (var slot = 0; slot < frame.SlotFlags.Length; slot++)
        {
            var (flags, values) = (frame.SlotFlags[slot], frame.SlotValues[slot]);
            Axis(stream, slot, (flags & 1) != 0, values.X);
            Axis(stream, slot, (flags & 2) != 0, values.Y);
            Axis(stream, slot, (flags & 4) != 0, values.Z);
        }
        return stream;
    }

    private static void Axis(MemoryStream stream, int slot, bool flagged, int? value)
    {
        if (flagged != value.HasValue) throw new InvalidOperationException($"Frame slot {slot}'s axis flags and values disagree.");
        if (value is { } stored) stream.SignedSmart(stored);
    }
}
