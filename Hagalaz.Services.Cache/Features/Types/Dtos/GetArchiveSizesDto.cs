namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record GetArchiveSizesDto(
        int Items,
        int Npcs,
        int Objects,
        int Sprites,
        int Quests,
        int Animations,
        int Graphics,
        int Cs2,
        int ClientMapDefinitions,
        int ConfigDefinitions,
        int VarpBits,
        int Maps);
