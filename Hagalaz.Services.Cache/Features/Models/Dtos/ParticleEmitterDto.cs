namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record ParticleEmitterDto(int TextureId, uint ColorMin, uint ColorMax, float LifeMin, float LifeMax,
    float RateMin, float RateMax, float SizeMin, float SizeMax, float SpeedMin, float SpeedMax, int[] Angles, int[] UnsupportedOpcodes);
