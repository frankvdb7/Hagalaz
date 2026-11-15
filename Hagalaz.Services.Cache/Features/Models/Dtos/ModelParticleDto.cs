namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record ModelParticleDto(int EmitterId, int Face, ParticleEmitterDto? Definition, string? Warning);
