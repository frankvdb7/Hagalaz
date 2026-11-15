namespace Hagalaz.Services.Cache.Features.Containers.Dtos;

public sealed record WriteContainerDto(string DataBase64, byte CompressionType = 0, short? Version = null);
