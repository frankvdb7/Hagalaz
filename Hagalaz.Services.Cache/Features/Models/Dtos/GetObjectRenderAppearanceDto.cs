namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record GetObjectRenderAppearanceDto(int Id, int Shape, int Rotation, int SizeX, int SizeY, int DecorDisplacement, int ContourMode, int ContourParameter, GetModelDto[] Parts, string[] Warnings);
