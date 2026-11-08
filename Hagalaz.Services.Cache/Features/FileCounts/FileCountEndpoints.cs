using Hagalaz.Cache.Abstractions;
using Hagalaz.Services.Cache.Features.FileCounts.Dtos;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.FileCounts;

public static class FileCountEndpoints
{
    public static IEndpointRouteBuilder MapFileCountEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/indexes/{indexId:int}/file-count", GetFileCount)
            .WithSummary("Count the files in an index");
        group.MapGet("/indexes/{indexId:int}/files/{fileId:int}/sub-file-count", GetSubFileCount)
            .WithSummary("Count the members of an archive");
        return group;
    }

    private static Results<Ok<GetFileCountDto>, BadRequest<Microsoft.AspNetCore.Mvc.ProblemDetails>> GetFileCount(int indexId, ICacheAPI cacheApi)
    {
        if (indexId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("indexId must be greater than or equal to 0."));
        }

        return TypedResults.Ok(new GetFileCountDto(cacheApi.GetFileCount(indexId)));
    }

    private static Results<Ok<GetSubFileCountDto>, BadRequest<Microsoft.AspNetCore.Mvc.ProblemDetails>> GetSubFileCount(
        int indexId,
        int fileId,
        ICacheAPI cacheApi)
    {
        if (indexId < 0 || fileId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("indexId and fileId must be greater than or equal to 0."));
        }

        return TypedResults.Ok(new GetSubFileCountDto(cacheApi.GetFileCount(indexId, fileId)));
    }
}
