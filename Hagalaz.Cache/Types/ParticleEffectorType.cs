using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types;

public sealed class ParticleEffectorType(int id) : IParticleEffectorType
{
    public int Id { get; } = id;
    public int? ConeAngle { get; set; }
    public int? Opcode2 { get; set; }
    public ParticleVector? Position { get; set; }
    public ParticleFalloff? Falloff { get; set; }
    public int? Scope { get; set; }
    public bool DirectDisplacement { get; set; }
    public bool Radial { get; set; }
    public bool Reverse { get; set; }

    public int EffectiveAttenuation => Falloff is { Attenuation: not 0 } falloff ? falloff.Attenuation : 1;
}
