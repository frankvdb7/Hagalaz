using System;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features;

public static class RawFileEndpoints
{
    public static IEndpointRouteBuilder MapRawFileEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/indexes/{indexId:int}/files/{fileId:int}/raw", GetRawFile);
        group.MapGet("/indexes/{indexId:int}/files/{fileId:int}/sub-files/{subFileId:int}/raw", GetRawSubFile);
        return group;
    }

    private static IResult GetRawFile(int indexId, int fileId, ICacheAPI cacheApi)
    {
        try
        {
            using var stream = cacheApi.Read(indexId, fileId);
            return TypedResults.File(stream.ToArray(), "application/octet-stream", $"index-{indexId}-file-{fileId}.bin");
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    private static IResult GetRawSubFile(int indexId, int fileId, int subFileId, ICacheAPI cacheApi)
    {
        try
        {
            using var stream = cacheApi.Read(indexId, fileId, subFileId);
            return TypedResults.File(stream.ToArray(), "application/octet-stream", $"index-{indexId}-file-{fileId}-sub-file-{subFileId}.bin");
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }
}
