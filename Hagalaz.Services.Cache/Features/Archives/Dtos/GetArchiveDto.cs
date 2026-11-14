using System.Collections.Generic;

namespace Hagalaz.Services.Cache.Features.Archives.Dtos;

public sealed record GetArchiveDto(int FileId, IReadOnlyList<ArchiveEntryDto> Entries);
