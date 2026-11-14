namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record GetGraphicDefinitionDto(int Id, int DefaultModelId, int AnimationId, int ResizeX, int ResizeY, int Rotation, int Ambient, int Contrast);
