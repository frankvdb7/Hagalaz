using System.Collections.Generic;
using Hagalaz.Cache.Types;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record UpdateMapLandscapeDto(IReadOnlyList<MapRenderPlacement>? Objects);
