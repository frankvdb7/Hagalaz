using System;
using System.Buffers.Binary;
using System.IO;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Reads the columnar material appearance fields from revision-742 index 26, archive 0.</summary>
public static class MaterialDefinitionDecoder
{
    public sealed record Material(int AlphaMode, int ColorBlend, int Brightness);

    public static Material Decode(ReadOnlySpan<byte> data, int id)
    {
        if (data.Length < 2) throw new InvalidDataException("Truncated material table.");
        var count = BinaryPrimitives.ReadUInt16BigEndian(data);
        if (data.Length < 2 + count) throw new InvalidDataException("Truncated material presence flags.");
        if (id < 0 || id >= count || data[2 + id] != 1) throw new NotSupportedException("The texture has no material definition.");
        var present = 0;
        var ordinal = 0;
        for (var index = 0; index < count; index++)
        {
            if (data[2 + index] != 1) continue;
            if (index < id) ordinal++;
            present++;
        }
        // Sixteen byte columns, one ushort column and one int column precede alpha mode.
        var offset = 2 + count + present * 22 + ordinal;
        if (offset >= data.Length) throw new InvalidDataException("Truncated material alpha modes.");
        var mode = data[offset];
        if (mode > 2) throw new NotSupportedException($"Unsupported material alpha mode {mode}.");
        var columns = 2 + count;
        return new Material(mode, data[columns + present * 4 + ordinal], data[columns + present * 3 + ordinal]);
    }
}
