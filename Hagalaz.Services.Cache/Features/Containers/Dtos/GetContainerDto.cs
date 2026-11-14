namespace Hagalaz.Services.Cache.Features.Containers.Dtos;

public sealed record GetContainerDto(
    byte CompressionType,
    short Version,
    bool IsVersioned,
    string DataBase64,
    int DataLength);
