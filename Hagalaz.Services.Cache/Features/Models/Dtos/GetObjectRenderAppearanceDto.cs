namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record GetObjectRenderAppearanceDto(int Id, int Shape, int Rotation, int SizeX, int SizeY, int DecorDisplacement, int ContourMode, int ContourParameter, GetModelDto[] Parts, string[] Warnings, string? Name = null, int ShadowStrength = 0,
    // Collision inputs of the placed definition, as the game server reads them.
    int ClipType = 0, bool Solid = false, bool Gateway = false);
