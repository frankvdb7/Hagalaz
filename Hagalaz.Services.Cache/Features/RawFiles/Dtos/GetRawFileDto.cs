using Microsoft.AspNetCore.Mvc;

namespace Hagalaz.Services.Cache.Features.RawFiles.Dtos;

public sealed class GetRawFileDto
{
    [FromRoute(Name = "indexId")]
    public int IndexId { get; init; }

    [FromRoute(Name = "fileId")]
    public int FileId { get; init; }
}
