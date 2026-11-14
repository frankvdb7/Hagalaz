namespace Hagalaz.Cache.Abstractions.Types;

/// <summary>
/// A particle emitter (index 27, archive 0). Nullable members are absent opcodes and values are as stored; opcodes whose meaning
/// is unknown are kept raw as <c>OpcodeN</c>. The read-only members derive what the model viewer uses.
/// </summary>
public interface IParticleEmitterType : IType
{
    /// <summary>Opcode 1: four spread angles, as stored (an eighth of the value used).</summary>
    int[]? Angles { get; set; }
    int? Opcode2 { get; set; }
    /// <summary>Opcode 3: minimum and maximum speed, as stored.</summary>
    ParticleRange? Speed { get; set; }
    /// <summary>Opcode 4: drag mode and strength bytes.</summary>
    ParticleRange? Drag { get; set; }
    /// <summary>Opcode 5: one size for minimum and maximum, as stored (a quarter of the value used).</summary>
    int? Size { get; set; }
    /// <summary>Opcode 6: minimum and maximum ARGB colour.</summary>
    ParticleRange? Colors { get; set; }
    /// <summary>Opcode 7: minimum and maximum lifetime in milliseconds.</summary>
    ParticleRange? Life { get; set; }
    /// <summary>Opcode 8: minimum and maximum emission rate, as stored.</summary>
    ParticleRange? Rate { get; set; }
    /// <summary>Opcode 9: local effector IDs.</summary>
    int[]? LocalEffectors { get; set; }
    /// <summary>Opcode 10: constant effector IDs.</summary>
    int[]? ConstantEffectors { get; set; }
    int? Opcode12 { get; set; }
    int? Opcode13 { get; set; }
    int? Opcode14 { get; set; }
    /// <summary>Opcode 15: texture ID, 65535 meaning none.</summary>
    int? TextureId { get; set; }
    /// <summary>Opcode 16: six bytes.</summary>
    byte[]? Opcode16 { get; set; }
    int? Opcode17 { get; set; }
    /// <summary>Opcode 18: target ARGB colour of the colour-over-life curve.</summary>
    int? TargetColor { get; set; }
    int? Opcode19 { get; set; }
    /// <summary>Opcode 20: lifetime percentage at which the RGB target is reached.</summary>
    int? ColorLifetimePercent { get; set; }
    /// <summary>Opcode 21: lifetime percentage at which the alpha target is reached.</summary>
    int? AlphaLifetimePercent { get; set; }
    /// <summary>Opcode 22: target speed, as stored.</summary>
    int? TargetSpeed { get; set; }
    /// <summary>Opcode 23: lifetime percentage at which the target speed is reached.</summary>
    int? SpeedLifetimePercent { get; set; }
    bool Opcode24 { get; set; }
    /// <summary>Opcode 25: a counted list of two-byte values.</summary>
    int[]? Opcode25 { get; set; }
    bool Opcode26 { get; set; }
    /// <summary>Opcode 27: target size of the size-over-life curve, as stored.</summary>
    int? TargetSize { get; set; }
    /// <summary>Opcode 28: lifetime percentage at which the target size is reached.</summary>
    int? SizeLifetimePercent { get; set; }
    int? Opcode29 { get; set; }
    bool Opcode30 { get; set; }
    /// <summary>Opcode 31: minimum and maximum size, as stored. Encoded after opcode 5, so it wins when both are present.</summary>
    ParticleRange? SizeRange { get; set; }
    bool Opcode32 { get; set; }
    bool Opcode33 { get; set; }
    bool Opcode34 { get; set; }

    int RenderTextureId { get; }
    uint ColorMin { get; }
    uint ColorMax { get; }
    float LifeMin { get; }
    float LifeMax { get; }
    float RateMin { get; }
    float RateMax { get; }
    float SizeMin { get; }
    float SizeMax { get; }
    float SpeedMin { get; }
    float SpeedMax { get; }
    int[] RenderAngles { get; }
    /// <summary>Opcodes the model viewer does not simulate, ascending.</summary>
    int[] UnsupportedOpcodes { get; }
    int[] LocalEffectorIds { get; }
    int[] ConstantEffectorIds { get; }
    ParticleDrag? SpeedDrag { get; }
    ParticleSizeCurve? SizeOverLife { get; }
    ParticleSpeedCurve? SpeedOverLife { get; }
    ParticleColorCurve? ColorOverLife { get; }
}

public readonly record struct ParticleRange(int Min, int Max);

public sealed record ParticleDrag(int Mode, int Strength);

public sealed record ParticleSizeCurve(float Target, int LifetimePercent);

public sealed record ParticleSpeedCurve(int RawTarget, int LifetimePercent);

public sealed record ParticleColorCurve(uint TargetArgb, int RgbLifetimePercent, int AlphaLifetimePercent);
