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
        group.MapPost("/store/indexes/{indexId:int}/files/{fileId:int}/actions/write-raw", WriteRawFile)
            .WithSummary("Write raw file bytes without updating metadata").Produces<WriteRawFileResultDto>();
        group.MapPost("/store/indexes/{indexId:int}/files/{fileId:int}/actions/copy-raw", CopyRawFile)
            .WithSummary("Copy raw file bytes without updating metadata").Produces<CopyRawFileResultDto>();
        return group;
    }

    private static async Task<IResult> WriteRawFile(int indexId, int fileId, WriteRawFileDto request, ICacheAPI cache, [FromServices] IOutputCacheStore outputCache)
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
        // A failed write may still have changed the store, matching CacheApi's own finally invalidation.
        try { cache.WriteRawFile(indexId, fileId, stream); }
        finally { await InvalidateDerivedReadsAsync(cache, outputCache); }
        return TypedResults.Ok(new WriteRawFileResultDto("write-raw", indexId, fileId));
    }

    private static async Task<IResult> CopyRawFile(int indexId, int fileId, CopyRawFileDto request, ICacheAPI cache, [FromServices] IOutputCacheStore outputCache)
    {
        if (indexId < 0 || fileId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("indexId and fileId must be greater than or equal to 0."));
        }

        if (request.SourceIndexId < 0 || request.SourceFileId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("sourceIndexId and sourceFileId must be greater than or equal to 0."));
        }

        try { cache.CopyRawFile(indexId, fileId, request.SourceIndexId, request.SourceFileId); }
        finally { await InvalidateDerivedReadsAsync(cache, outputCache); }
        return TypedResults.Ok(new CopyRawFileResultDto("copy-raw", indexId, fileId, request.SourceIndexId, request.SourceFileId));
    }

    // Raw writes bypass reference-table metadata, so drop read models derived from it.
    private static async Task InvalidateDerivedReadsAsync(ICacheAPI cache, IOutputCacheStore outputCache)
    {
        ObjectDefinitionsEndpoints.InvalidateCatalog();
        ItemIcons.InvalidateValidators(cache);
        await outputCache.EvictByTagAsync(TextureEndpoints.ImageCacheTag, CancellationToken.None);
    }
}
