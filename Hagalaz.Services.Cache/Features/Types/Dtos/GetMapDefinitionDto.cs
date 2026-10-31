using System.Collections.Generic;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record GetMapDefinitionDto(int Id, int TerrainPlanes, int TerrainWidth, int TerrainHeight, int ObjectCount, IReadOnlyList<MapObjectDto> Objects);
