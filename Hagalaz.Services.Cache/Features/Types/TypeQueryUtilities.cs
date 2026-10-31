using System.Linq;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Http;

namespace Hagalaz.Services.Cache.Features.Types;

internal static class TypeQueryUtilities
{
    internal static IResult? ValidateRange(int startId, int endIdExclusive, int archiveSize)
    {
        if (startId < 0 || endIdExclusive < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("startId and endIdExclusive must be greater than or equal to 0."));
        }

        if (endIdExclusive <= startId)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("endIdExclusive must be greater than startId."));
        }

        if (archiveSize > 0 && endIdExclusive > archiveSize)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation($"endIdExclusive must be less than or equal to archive size {archiveSize}."));
        }

        return null;
    }

    internal static IResult? ValidateSearch(string? query, int offset, int limit, int archiveSize, bool requireQuery)
    {
        if (archiveSize <= 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("Archive size is 0. Search is not supported for this type."));
        }

        if (requireQuery && string.IsNullOrWhiteSpace(query))
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("query is required."));
        }

        if (offset < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("offset must be greater than or equal to 0."));
        }

        if (limit <= 0 || limit > 200)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("limit must be between 1 and 200."));
        }

        return null;
    }

    internal static IResult CreateSearchResponse<T>(string query, int offset, int limit, T[] matches)
    {
        var page = matches.Skip(offset).Take(limit).ToArray();
        return TypedResults.Ok(new SearchDefinitionsDto<T>(query, matches.Length, offset, limit, page));
    }
}
