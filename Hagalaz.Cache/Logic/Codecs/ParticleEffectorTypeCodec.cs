using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Revision-742 Class208 particle effectors, index 27 archive 1. Encodes opcodes in ascending order.</summary>
public sealed class ParticleEffectorTypeCodec : ITypeCodec<IParticleEffectorType>
{
    public IParticleEffectorType Decode(int id, MemoryStream stream)
    {
        var type = new ParticleEffectorType(id);
        var reader = new DefinitionReader(stream, "particle effector");
        for (var opcode = reader.Byte(); opcode != 0; opcode = reader.Byte())
        {
            switch (opcode)
            {
                case 1: type.ConeAngle = reader.UShort(); break;
                case 2: type.Opcode2 = reader.Byte(); break;
                case 3: type.Position = new(reader.Int(), reader.Int(), reader.Int()); break;
                case 4: type.Falloff = new(reader.Byte(), reader.Int()); break;
                case 6: type.Scope = reader.Byte(); break;
                case 8: type.DirectDisplacement = true; break;
                case 9: type.Radial = true; break;
                case 10: type.Reverse = true; break;
                default: throw reader.UnknownOpcode(opcode);
            }
        }
        return type;
    }

    public MemoryStream Encode(IParticleEffectorType type)
    {
        var stream = new MemoryStream();
        if (type.ConeAngle is { } angle) { stream.Byte(1); stream.UShort(angle); }
        if (type.Opcode2 is { } opcode2) { stream.Byte(2); stream.Byte(opcode2); }
        if (type.Position is { } position) { stream.Byte(3); stream.Int(position.X); stream.Int(position.Y); stream.Int(position.Z); }
        if (type.Falloff is { } falloff) { stream.Byte(4); stream.Byte(falloff.Falloff); stream.Int(falloff.Attenuation); }
        if (type.Scope is { } scope) { stream.Byte(6); stream.Byte(scope); }
        if (type.DirectDisplacement) stream.Byte(8);
        if (type.Radial) stream.Byte(9);
        if (type.Reverse) stream.Byte(10);
        stream.Byte(0);
        return stream;
    }
}
