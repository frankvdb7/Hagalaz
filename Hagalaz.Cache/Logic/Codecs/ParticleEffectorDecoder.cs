using System.IO;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Revision-742 Class208 definitions, index 27 archive 1.</summary>
public static class ParticleEffectorDecoder
{
    public sealed record Definition(int X, int Y, int Z, int ConeAngle, int Falloff, int Attenuation, int Scope,
        bool DirectDisplacement, bool Radial, bool Reverse);

    public static Definition Decode(Stream data)
    {
        using var reader = new BinaryReader(data, System.Text.Encoding.UTF8, leaveOpen: true);
        int x = 0, y = 0, z = 0, angle = 0, falloff = 0, attenuation = 1, scope = 0;
        bool direct = false, radial = false, reverse = false;
        while (true)
        {
            var opcode = reader.ReadByte();
            switch (opcode)
            {
                case 0: return new(x, y, z, angle, falloff, attenuation == 0 ? 1 : attenuation, scope, direct, radial, reverse);
                case 1: angle = (reader.ReadByte() << 8) | reader.ReadByte(); break;
                case 2: reader.ReadByte(); break;
                case 3: x = ReadInt(); y = ReadInt(); z = ReadInt(); break;
                case 4: falloff = reader.ReadByte(); attenuation = ReadInt(); break;
                case 6: scope = reader.ReadByte(); break;
                case 8: direct = true; break;
                case 9: radial = true; break;
                case 10: reverse = true; break;
                default: throw new InvalidDataException($"Unknown particle effector opcode {opcode}.");
            }
        }

        int ReadInt() => System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(reader.ReadInt32());
    }
}
