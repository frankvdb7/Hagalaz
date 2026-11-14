using System.Collections.Generic;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record RefreshWorldMapDto(IReadOnlyList<int>? Regions);
