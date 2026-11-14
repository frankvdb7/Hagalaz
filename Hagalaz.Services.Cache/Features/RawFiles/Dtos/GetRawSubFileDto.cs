using Microsoft.AspNetCore.Mvc;

namespace Hagalaz.Services.Cache.Features.RawFiles.Dtos;

public sealed class GetRawSubFileDto
{
    [FromRoute(Name = "indexId")]
    public int IndexId { get; init; }

    [FromRoute(Name = "fileId")]
    public int FileId { get; init; }

    [FromRoute(Name = "subFileId")]
    public int SubFileId { get; init; }
}
