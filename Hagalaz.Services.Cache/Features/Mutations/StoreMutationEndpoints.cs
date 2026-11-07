using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Services.Cache.Features.Mutations.Dtos;
using Hagalaz.Services.Cache.Features.Models;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.Mutations;

public static class StoreMutationEndpoints
{
    public static IEndpointRouteBuilder MapStoreMutationEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapPost("/store/indexes/{indexId:int}/files/{fileId:int}/actions/write-raw", WriteRawFile);
        group.MapPost("/store/indexes/{indexId:int}/files/{fileId:int}/actions/copy-raw", CopyRawFile);
        return group;
    }

    private static async Task<IResult> WriteRawFile(int indexId, int fileId, WriteRawFileDto request, ICacheAPI cache, [FromServices] IOutputCacheStore outputCache, CancellationToken cancellationToken)
    {
        if (indexId < 0 || fileId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("indexId and fileId must be greater than or equal to 0."));
        }

        if (string.IsNullOrWhiteSpace(request.DataBase64))
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("dataBase64 is required."));
        }

        byte[] data;
        try
        {
            data = Convert.FromBase64String(request.DataBase64);
        }
        catch (FormatException)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("dataBase64 is not a valid base64 string."));
        }

        using var stream = new MemoryStream(data);
        cache.WriteRawFile(indexId, fileId, stream);
        await InvalidateDerivedReadsAsync(outputCache, cancellationToken);
        return TypedResults.Ok(new WriteRawFileResultDto("write-raw", indexId, fileId));
    }

    private static async Task<IResult> CopyRawFile(int indexId, int fileId, CopyRawFileDto request, ICacheAPI cache, [FromServices] IOutputCacheStore outputCache, CancellationToken cancellationToken)
    {
        if (indexId < 0 || fileId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("indexId and fileId must be greater than or equal to 0."));
        }

        if (request.SourceIndexId < 0 || request.SourceFileId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("sourceIndexId and sourceFileId must be greater than or equal to 0."));
        }

        cache.CopyRawFile(indexId, fileId, request.SourceIndexId, request.SourceFileId);
        await InvalidateDerivedReadsAsync(outputCache, cancellationToken);
        return TypedResults.Ok(new CopyRawFileResultDto("copy-raw", indexId, fileId, request.SourceIndexId, request.SourceFileId));
    }

    // Raw writes bypass reference-table metadata, so drop read models derived from it.
    private static async Task InvalidateDerivedReadsAsync(IOutputCacheStore outputCache, CancellationToken cancellationToken)
    {
        ObjectDefinitionsEndpoints.InvalidateCatalog();
        await outputCache.EvictByTagAsync(TextureEndpoints.ImageCacheTag, cancellationToken);
    }
}
