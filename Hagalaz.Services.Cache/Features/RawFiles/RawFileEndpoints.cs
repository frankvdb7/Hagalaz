using Hagalaz.Cache.Abstractions;
using Hagalaz.Services.Cache.Features.RawFiles.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.RawFiles;

public static class RawFileEndpoints
{
    public static IEndpointRouteBuilder MapRawFileEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/indexes/{indexId:int}/files/{fileId:int}/raw", GetRawFile)
            .WithSummary("Download a file's raw bytes").Produces<System.IO.Stream>(StatusCodes.Status200OK, "application/octet-stream");
        group.MapGet("/indexes/{indexId:int}/files/{fileId:int}/sub-files/{subFileId:int}/raw", GetRawSubFile)
            .WithSummary("Download an archive member's raw bytes").Produces<System.IO.Stream>(StatusCodes.Status200OK, "application/octet-stream");
        return group;
    }

    private static IResult GetRawFile([AsParameters] GetRawFileDto request, ICacheAPI cacheApi)
    {
        using var stream = cacheApi.Read(request.IndexId, request.FileId);
        return TypedResults.File(stream.ToArray(), "application/octet-stream", $"index-{request.IndexId}-file-{request.FileId}.bin");
    }

    private static IResult GetRawSubFile([AsParameters] GetRawSubFileDto request, ICacheAPI cacheApi)
    {
        using var stream = cacheApi.Read(request.IndexId, request.FileId, request.SubFileId);
        return TypedResults.File(stream.ToArray(), "application/octet-stream", $"index-{request.IndexId}-file-{request.FileId}-sub-file-{request.SubFileId}.bin");
    }
}
