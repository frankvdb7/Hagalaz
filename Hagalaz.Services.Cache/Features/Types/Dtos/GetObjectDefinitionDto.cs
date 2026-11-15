namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record GetObjectDefinitionDto(
        int Id,
        string Name,
        int SizeX,
        int SizeY,
        bool Solid,
        bool Gateway,
        int ClipType,
        byte Surroundings,
        string?[] Actions,
        int VarpBitFileId);
