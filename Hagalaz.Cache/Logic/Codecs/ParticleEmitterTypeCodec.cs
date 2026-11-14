using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>
/// Revision-742 particle emitters, index 27 archive 0. Encodes opcodes in ascending order; a repeated opcode keeps its last value,
/// as the client does.
/// </summary>
public sealed class ParticleEmitterTypeCodec : ITypeCodec<IParticleEmitterType>
{
    public IParticleEmitterType Decode(int id, MemoryStream stream)
    {
        var type = new ParticleEmitterType(id);
        var r = new DefinitionReader(stream, "particle emitter definition");
        for (var opcode = r.Byte(); opcode != 0; opcode = r.Byte())
        {
            switch (opcode)
            {
                case 1: type.Angles = [r.UShort(), r.UShort(), r.UShort(), r.UShort()]; break;
                case 2: type.Opcode2 = r.Byte(); break;
                case 3: type.Speed = new(r.Int(), r.Int()); break;
                case 4: type.Drag = new(r.Byte(), r.Byte()); break;
                case 5: type.Size = r.UShort(); break;
                case 6: type.Colors = new(r.Int(), r.Int()); break;
                case 7: type.Life = new(r.UShort(), r.UShort()); break;
                case 8: type.Rate = new(r.UShort(), r.UShort()); break;
                case 9: type.LocalEffectors = Ids(r); break;
                case 10: type.ConstantEffectors = Ids(r); break;
                case 12: type.Opcode12 = r.Byte(); break;
                case 13: type.Opcode13 = r.Byte(); break;
                case 14: type.Opcode14 = r.UShort(); break;
                case 15: type.TextureId = r.UShort(); break;
                case 16: type.Opcode16 = r.Bytes(6); break;
                case 17: type.Opcode17 = r.UShort(); break;
                case 18: type.TargetColor = r.Int(); break;
                case 19: type.Opcode19 = r.Byte(); break;
                case 20: type.ColorLifetimePercent = r.Byte(); break;
                case 21: type.AlphaLifetimePercent = r.Byte(); break;
                case 22: type.TargetSpeed = r.Int(); break;
                case 23: type.SpeedLifetimePercent = r.Byte(); break;
                case 24: type.Opcode24 = true; break;
                case 25: type.Opcode25 = Ids(r); break;
                case 26: type.Opcode26 = true; break;
                case 27: type.TargetSize = r.UShort(); break;
                case 28: type.SizeLifetimePercent = r.Byte(); break;
                case 29: type.Opcode29 = r.UShort(); break;
                case 30: type.Opcode30 = true; break;
                case 31: type.SizeRange = new(r.UShort(), r.UShort()); break;
                case 32: type.Opcode32 = true; break;
                case 33: type.Opcode33 = true; break;
                case 34: type.Opcode34 = true; break;
                default: throw new System.NotSupportedException($"Unsupported particle emitter opcode {opcode}.");
            }
        }
        return type;
    }

    public MemoryStream Encode(IParticleEmitterType type)
    {
        var s = new MemoryStream();
        if (type.Angles is { } angles) { s.Byte(1); foreach (var angle in angles) s.UShort(angle); }
        Byte(s, 2, type.Opcode2);
        Ints(s, 3, type.Speed);
        if (type.Drag is { } drag) { s.Byte(4); s.Byte(drag.Min); s.Byte(drag.Max); }
        UShort(s, 5, type.Size);
        Ints(s, 6, type.Colors);
        UShorts(s, 7, type.Life);
        UShorts(s, 8, type.Rate);
        Ids(s, 9, type.LocalEffectors);
        Ids(s, 10, type.ConstantEffectors);
        Byte(s, 12, type.Opcode12);
        Byte(s, 13, type.Opcode13);
        UShort(s, 14, type.Opcode14);
        UShort(s, 15, type.TextureId);
        if (type.Opcode16 is { } opcode16) { s.Byte(16); s.Write(opcode16); }
        UShort(s, 17, type.Opcode17);
        Int(s, 18, type.TargetColor);
        Byte(s, 19, type.Opcode19);
        Byte(s, 20, type.ColorLifetimePercent);
        Byte(s, 21, type.AlphaLifetimePercent);
        Int(s, 22, type.TargetSpeed);
        Byte(s, 23, type.SpeedLifetimePercent);
        Flag(s, 24, type.Opcode24);
        Ids(s, 25, type.Opcode25);
        Flag(s, 26, type.Opcode26);
        UShort(s, 27, type.TargetSize);
        Byte(s, 28, type.SizeLifetimePercent);
        UShort(s, 29, type.Opcode29);
        Flag(s, 30, type.Opcode30);
        UShorts(s, 31, type.SizeRange);
        Flag(s, 32, type.Opcode32);
        Flag(s, 33, type.Opcode33);
        Flag(s, 34, type.Opcode34);
        s.Byte(0);
        return s;
    }

    private static int[] Ids(DefinitionReader reader)
    {
        var ids = new int[reader.Byte()];
        for (var index = 0; index < ids.Length; index++) ids[index] = reader.UShort();
        return ids;
    }

    private static void Ids(MemoryStream s, int opcode, int[]? ids)
    {
        if (ids is null) return;
        s.Byte(opcode);
        s.Byte(ids.Length);
        foreach (var id in ids) s.UShort(id);
    }

    private static void Flag(MemoryStream s, int opcode, bool present) { if (present) s.Byte(opcode); }
    private static void Byte(MemoryStream s, int opcode, int? value) { if (value is { } v) { s.Byte(opcode); s.Byte(v); } }
    private static void UShort(MemoryStream s, int opcode, int? value) { if (value is { } v) { s.Byte(opcode); s.UShort(v); } }
    private static void Int(MemoryStream s, int opcode, int? value) { if (value is { } v) { s.Byte(opcode); s.Int(v); } }
    private static void UShorts(MemoryStream s, int opcode, ParticleRange? range) { if (range is { } r) { s.Byte(opcode); s.UShort(r.Min); s.UShort(r.Max); } }
    private static void Ints(MemoryStream s, int opcode, ParticleRange? range) { if (range is { } r) { s.Byte(opcode); s.Int(r.Min); s.Int(r.Max); } }
}
