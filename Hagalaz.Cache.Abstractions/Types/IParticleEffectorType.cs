namespace Hagalaz.Cache.Abstractions.Types;

/// <summary>A particle effector (index 27, archive 1). Nullable members are absent opcodes; values are as stored.</summary>
public interface IParticleEffectorType : IType
{
    /// <summary>Opcode 1: cone angle.</summary>
    int? ConeAngle { get; set; }
    /// <summary>Opcode 2: a byte the client reads but whose meaning is unknown.</summary>
    int? Opcode2 { get; set; }
    /// <summary>Opcode 3: position.</summary>
    ParticleVector? Position { get; set; }
    /// <summary>Opcode 4: falloff and attenuation, attenuation as stored (0 is used as 1).</summary>
    ParticleFalloff? Falloff { get; set; }
    /// <summary>Opcode 6: scope.</summary>
    int? Scope { get; set; }
    /// <summary>Opcode 8.</summary>
    bool DirectDisplacement { get; set; }
    /// <summary>Opcode 9.</summary>
    bool Radial { get; set; }
    /// <summary>Opcode 10.</summary>
    bool Reverse { get; set; }

    /// <summary>The attenuation the client uses: the stored value, or 1 when absent or 0.</summary>
    int EffectiveAttenuation { get; }
}

public readonly record struct ParticleVector(int X, int Y, int Z);

public readonly record struct ParticleFalloff(int Falloff, int Attenuation);
