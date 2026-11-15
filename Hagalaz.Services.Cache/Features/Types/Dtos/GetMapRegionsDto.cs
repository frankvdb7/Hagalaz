using System.Collections.Generic;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record MapRegionSummaryDto(int Id, int RegionX, int RegionY, int? TerrainFileId, int? LandscapeFileId);

public sealed record GetMapRegionsDto(IReadOnlyList<MapRegionSummaryDto> Items, int Total, int Offset, int Limit);
