namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record ReplaceSpriteImageResultDto(string Operation, string Type, int Id, int Width, int Height, int FrameCount);
