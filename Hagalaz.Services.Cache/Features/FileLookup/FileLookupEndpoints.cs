using Hagalaz.Cache.Abstractions;
using Hagalaz.Services.Cache.Features.FileLookup.Dtos;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.FileLookup;

public static class FileLookupEndpoints
{
    public static IEndpointRouteBuilder MapFileLookupEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/indexes/{indexId:int}/files/id", GetFileId)
            .WithSummary("Find a file ID by its name hash");
        return group;
    }

    private static Results<Ok<GetFileIdDto>, BadRequest<Microsoft.AspNetCore.Mvc.ProblemDetails>> GetFileId(
        int indexId,
        string fileName,
        ICacheAPI cacheApi)
    {
        if (indexId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("indexId must be greater than or equal to 0."));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("fileName is required."));
        }

        return TypedResults.Ok(new GetFileIdDto(cacheApi.GetFileId(indexId, fileName)));
    }
}
