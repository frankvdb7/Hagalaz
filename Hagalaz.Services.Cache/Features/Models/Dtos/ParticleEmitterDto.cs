namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record ParticleEmitterDto(int TextureId, uint ColorMin, uint ColorMax, float LifeMin, float LifeMax,
    float RateMin, float RateMax, float SizeMin, float SizeMax, float SpeedMin, float SpeedMax, int[] Angles, int[] UnsupportedOpcodes, int[]? LocalEffectorIds = null, ParticleDragDto? SpeedDrag = null,
    int[]? ConstantEffectorIds = null, ParticleEffectorDto[]? ConstantEffectors = null, ParticleSizeCurveDto? SizeOverLife = null,
    ParticleSpeedCurveDto? SpeedOverLife = null, ParticleColorCurveDto? ColorOverLife = null);

public sealed record ParticleDragDto(int Mode, int Strength);

public sealed record ParticleSizeCurveDto(float Target, int LifetimePercent);
public sealed record ParticleSpeedCurveDto(int RawTarget, int LifetimePercent);
public sealed record ParticleColorCurveDto(uint TargetArgb, int RgbLifetimePercent, int AlphaLifetimePercent);
