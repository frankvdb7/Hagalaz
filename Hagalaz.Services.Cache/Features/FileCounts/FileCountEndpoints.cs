using Hagalaz.Cache.Abstractions;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features;

public static class FileCountEndpoints
{
    public static IEndpointRouteBuilder MapFileCountEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/indexes/{indexId:int}/file-count", GetFileCount);
        group.MapGet("/indexes/{indexId:int}/files/{fileId:int}/sub-file-count", GetSubFileCount);
        return group;
    }

    private static Results<Ok<FileCountResponse>, BadRequest<Microsoft.AspNetCore.Mvc.ProblemDetails>> GetFileCount(int indexId, ICacheAPI cacheApi)
    {
        if (indexId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("indexId must be greater than or equal to 0."));
        }

        return TypedResults.Ok(new FileCountResponse(cacheApi.GetFileCount(indexId)));
    }

    private static Results<Ok<FileCountResponse>, BadRequest<Microsoft.AspNetCore.Mvc.ProblemDetails>> GetSubFileCount(
        int indexId,
        int fileId,
        ICacheAPI cacheApi)
    {
        if (indexId < 0 || fileId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("indexId and fileId must be greater than or equal to 0."));
        }

        return TypedResults.Ok(new FileCountResponse(cacheApi.GetFileCount(indexId, fileId)));
    }

    public sealed record FileCountResponse(int Count);
}
