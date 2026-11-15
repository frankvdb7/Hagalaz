namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record GetNpcDefinitionDto(int Id, string Name, int CombatLevel, int Size, sbyte SpawnFaceDirection, int RenderId);
