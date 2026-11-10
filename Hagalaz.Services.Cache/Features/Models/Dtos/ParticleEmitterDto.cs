namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record ParticleEmitterDto(int TextureId, uint ColorMin, uint ColorMax, float LifeMin, float LifeMax,
    float RateMin, float RateMax, float SizeMin, float SizeMax, float SpeedMin, float SpeedMax, int[] Angles, int[] UnsupportedOpcodes, int[]? LocalEffectorIds = null, ParticleDragDto? SpeedDrag = null);

public sealed record ParticleDragDto(int Mode, int Strength);
