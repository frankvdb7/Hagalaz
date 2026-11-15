using System.Collections.Generic;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record SearchDefinitionsDto<T>(string Query, int TotalMatches, int Offset, int Limit, IReadOnlyList<T> Items);
