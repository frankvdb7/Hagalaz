using System.Collections.Generic;

namespace Hagalaz.Services.Cache.Features.Checksums.Dtos;

public sealed record GetChecksumTableDto(IReadOnlyList<ChecksumTableEntryDto> Entries);
