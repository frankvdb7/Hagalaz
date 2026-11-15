using System.Collections.Generic;

namespace Hagalaz.Services.Cache.Features.ReferenceTables.Dtos;

public sealed record GetReferenceTableDto(int Version, byte Protocol, byte Flags, int Capacity, IReadOnlyList<ReferenceTableEntryDto> Entries, int Count);
