using System.Linq;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types;

public sealed class ParticleEmitterType(int id) : IParticleEmitterType
{
    public int Id { get; } = id;
    public int[]? Angles { get; set; }
    public int? Opcode2 { get; set; }
    public ParticleRange? Speed { get; set; }
    public ParticleRange? Drag { get; set; }
    public int? Size { get; set; }
    public ParticleRange? Colors { get; set; }
    public ParticleRange? Life { get; set; }
    public ParticleRange? Rate { get; set; }
    public int[]? LocalEffectors { get; set; }
    public int[]? ConstantEffectors { get; set; }
    public int? Opcode12 { get; set; }
    public int? Opcode13 { get; set; }
    public int? Opcode14 { get; set; }
    public int? TextureId { get; set; }
    public byte[]? Opcode16 { get; set; }
    public int? Opcode17 { get; set; }
    public int? TargetColor { get; set; }
    public int? Opcode19 { get; set; }
    public int? ColorLifetimePercent { get; set; }
    public int? AlphaLifetimePercent { get; set; }
    public int? TargetSpeed { get; set; }
    public int? SpeedLifetimePercent { get; set; }
    public bool Opcode24 { get; set; }
    public int[]? Opcode25 { get; set; }
    public bool Opcode26 { get; set; }
    public int? TargetSize { get; set; }
    public int? SizeLifetimePercent { get; set; }
    public int? Opcode29 { get; set; }
    public bool Opcode30 { get; set; }
    public ParticleRange? SizeRange { get; set; }
    public bool Opcode32 { get; set; }
    public bool Opcode33 { get; set; }
    public bool Opcode34 { get; set; }

    public int RenderTextureId => TextureId is null or 65535 ? -1 : TextureId.Value;
    public uint ColorMin => unchecked((uint)(Colors?.Min ?? 0));
    public uint ColorMax => unchecked((uint)(Colors?.Max ?? 0));
    public float LifeMin => (Life?.Min ?? 0) / 1000f;
    public float LifeMax => (Life?.Max ?? 0) / 1000f;
    public float RateMin => (Rate?.Min ?? 0) * 1000f / 64;
    public float RateMax => (Rate?.Max ?? 0) * 1000f / 64;
    public float SizeMin => SizeRange is { } range ? range.Min * 4 : (Size ?? 0) * 4;
    public float SizeMax => SizeRange is { } range ? range.Max * 4 : (Size ?? 0) * 4;
    public float SpeedMin => (Speed?.Min ?? 0) / 262144f * 1000;
    public float SpeedMax => (Speed?.Max ?? 0) / 262144f * 1000;
    public int[] RenderAngles => Angles?.Select(angle => angle * 8).ToArray() ?? new int[4];
    public int[] LocalEffectorIds => LocalEffectors ?? [];
    public int[] ConstantEffectorIds => ConstantEffectors ?? [];
    public ParticleDrag? SpeedDrag => Drag is { Min: 1 or 2 } drag ? new(drag.Min, (sbyte)drag.Max) : null;
    public ParticleSizeCurve? SizeOverLife => TargetSize is { } target ? new(target * 4, SizeLifetimePercent ?? 100) : null;
    public ParticleSpeedCurve? SpeedOverLife => TargetSpeed is { } target and not -1 ? new(target, SpeedLifetimePercent ?? 100) : null;
    public ParticleColorCurve? ColorOverLife => TargetColor is { } target and not 0
        ? new(unchecked((uint)target), ColorLifetimePercent ?? 100, AlphaLifetimePercent ?? 100)
        : null;

    public int[] UnsupportedOpcodes => new (int Opcode, bool Present)[]
    {
        (2, Opcode2 is not null), (4, Drag is { Min: not (0 or 1 or 2) }), (12, Opcode12 is not null), (13, Opcode13 is not null),
        (14, Opcode14 is not null), (16, Opcode16 is not null), (17, Opcode17 is not null), (19, Opcode19 is not null), (24, Opcode24),
        (25, Opcode25 is not null), (26, Opcode26), (29, Opcode29 is not null), (30, Opcode30), (32, Opcode32), (33, Opcode33), (34, Opcode34),
    }.Where(entry => entry.Present).Select(entry => entry.Opcode).ToArray();
}
