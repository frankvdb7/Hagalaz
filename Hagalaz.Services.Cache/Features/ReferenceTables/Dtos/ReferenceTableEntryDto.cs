using System.Collections.Generic;

namespace Hagalaz.Services.Cache.Features.ReferenceTables.Dtos;

public sealed record ReferenceTableEntryDto(int FileId, int Id, int Crc32, int Version, int Capacity, string WhirlpoolDigestBase64, IReadOnlyList<ReferenceTableChildEntryDto> Children);
