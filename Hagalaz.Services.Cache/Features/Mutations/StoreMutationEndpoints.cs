using System;
using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features;

public static class StoreMutationEndpoints
{
    public static IEndpointRouteBuilder MapStoreMutationEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapPost("/store/indexes/{indexId:int}/files/{fileId:int}/actions/write-raw", WriteRawFile);
        group.MapPost("/store/indexes/{indexId:int}/files/{fileId:int}/actions/copy-raw", CopyRawFile);
        return group;
    }

    private static IResult WriteRawFile(int indexId, int fileId, WriteRawFileRequest request, ICacheAPI cache)
    {
        if (string.IsNullOrWhiteSpace(request.DataBase64))
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("dataBase64 is required."));
        }

        try
        {
            var data = Convert.FromBase64String(request.DataBase64);
            using var stream = new MemoryStream(data);
            cache.WriteRawFile(indexId, fileId, stream);
            return TypedResults.Ok(new StoreMutationResponse("write-raw", indexId, fileId));
        }
        catch (FormatException)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("dataBase64 is not a valid base64 string."));
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    private static IResult CopyRawFile(int indexId, int fileId, CopyRawFileRequest request, ICacheAPI cache)
    {
        if (request.SourceIndexId < 0 || request.SourceFileId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("sourceIndexId and sourceFileId must be greater than or equal to 0."));
        }

        try
        {
            cache.CopyRawFile(indexId, fileId, request.SourceIndexId, request.SourceFileId);

            return TypedResults.Ok(new StoreMutationResponse("copy-raw", indexId, fileId, request.SourceIndexId, request.SourceFileId));
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    public sealed record WriteRawFileRequest(string DataBase64);

    public sealed record CopyRawFileRequest(int SourceIndexId, int SourceFileId);

    public sealed record StoreMutationResponse(string Action, int IndexId, int FileId, int? SourceIndexId = null, int? SourceFileId = null);
}
