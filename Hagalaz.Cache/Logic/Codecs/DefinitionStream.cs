using System;
using System.IO;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Bounds-checked big-endian reads for definition codecs: running out of data is invalid data, never a default value.</summary>
internal sealed class DefinitionReader(MemoryStream stream, string format)
{
    public int Byte()
    {
        var value = stream.ReadByte();
        return value >= 0 ? value : throw Truncated();
    }

    public int UShort() => (Byte() << 8) | Byte();

    public int Int() => (Byte() << 24) | (Byte() << 16) | (Byte() << 8) | Byte();

    /// <summary>A 2-byte value below 32768, or a 4-byte value with its top bit set.</summary>
    public int BigSmart()
    {
        var first = Byte();
        return first < 128 ? (first << 8) | Byte() : ((first & 0x7F) << 24) | (Byte() << 16) | (Byte() << 8) | Byte();
    }

    /// <summary>A 1-byte value of -64..63, or a 2-byte value with its top bit set.</summary>
    public int SignedSmart()
    {
        var first = Byte();
        return first < 128 ? first - 64 : ((first << 8) | Byte()) - 49152;
    }

    /// <summary>A 1-byte value below 128, or a 2-byte value with its top bit set.</summary>
    public int UnsignedSmart()
    {
        var first = Byte();
        return first < 128 ? first : ((first << 8) | Byte()) - 32768;
    }

    /// <summary>Fails when the stream holds data after the definition.</summary>
    public void End()
    {
        if (stream.Position != stream.Length) throw new InvalidDataException($"Unexpected {format} trailing data.");
    }

    public byte[] Bytes(int count)
    {
        var bytes = new byte[count];
        for (var index = 0; index < count; index++) bytes[index] = (byte)Byte();
        return bytes;
    }

    public InvalidDataException Truncated() => new($"Truncated {format}.");

    public InvalidDataException UnknownOpcode(int opcode) => new($"Unknown {format} opcode {opcode}.");
}

/// <summary>Big-endian writes matching <see cref="DefinitionReader"/>.</summary>
internal static class DefinitionWriter
{
    public static void Byte(this MemoryStream stream, int value) => stream.WriteByte((byte)value);

    public static void UShort(this MemoryStream stream, int value)
    {
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    public static void Medium(this MemoryStream stream, int value)
    {
        stream.WriteByte((byte)(value >> 16));
        stream.UShort(value);
    }

    public static void Int(this MemoryStream stream, int value)
    {
        stream.UShort(value >> 16);
        stream.UShort(value);
    }

    public static void SignedSmart(this MemoryStream stream, int value)
    {
        if (value is >= -64 and < 64) stream.Byte(value + 64);
        else if (value is >= -16384 and < 16384) stream.UShort(value + 49152);
        else throw new InvalidOperationException($"{value} does not fit a signed smart value.");
    }

    public static void UnsignedSmart(this MemoryStream stream, int value)
    {
        if (value is >= 0 and < 128) stream.Byte(value);
        else if (value is >= 128 and < 32768) stream.UShort(value + 32768);
        else throw new InvalidOperationException($"{value} does not fit an unsigned smart value.");
    }

    public static void BigSmart(this MemoryStream stream, int value)
    {
        if (value is >= 0 and < 32768) stream.UShort(value);
        else stream.Int(value | int.MinValue);
    }
}
