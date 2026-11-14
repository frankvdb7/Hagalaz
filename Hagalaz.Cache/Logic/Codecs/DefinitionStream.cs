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

    public static void Int(this MemoryStream stream, int value)
    {
        stream.UShort(value >> 16);
        stream.UShort(value);
    }

    public static void BigSmart(this MemoryStream stream, int value)
    {
        if (value is >= 0 and < 32768) stream.UShort(value);
        else stream.Int(value | int.MinValue);
    }
}
