using Hagalaz.Services.Cache.Features.Containers.Dtos;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features.Models;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.Containers;

public static class ContainerEndpoints
{
    public static IEndpointRouteBuilder MapContainerEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/indexes/{indexId:int}/files/{fileId:int}/container", GetContainer).WithSummary("Read and decompress a cache container").Produces<GetContainerDto>();
        group.MapPost("/indexes/{indexId:int}/files/{fileId:int}/container/decrypt", GetContainerWithXtea).WithSummary("Read a container decrypted with XTEA keys").Produces<GetContainerDto>();
        group.MapPost("/indexes/{indexId:int}/files/{fileId:int}/actions/write-container", WriteContainer).WithSummary("Write a container and update the reference table").Produces<WriteContainerResultDto>();
        return group;
    }

    private static IResult GetContainer(int indexId, int fileId, ICacheAPI cacheApi)
    {
        using var container = cacheApi.ReadContainer(indexId, fileId);
        return TypedResults.Ok(MapContainer(container));
    }

    private static IResult GetContainerWithXtea(int indexId, int fileId, DecryptContainerDto request, ICacheAPI cacheApi)
    {
        if (request.XteaKeys is null || request.XteaKeys.Length != 4)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("xteaKeys must contain exactly 4 integers."));
        }

        using var container = cacheApi.ReadContainer(indexId, fileId, request.XteaKeys);
        return TypedResults.Ok(MapContainer(container));
    }

    private static async Task<IResult> WriteContainer(int indexId, int fileId, WriteContainerDto request, ICacheAPI cacheApi, [FromServices] IOutputCacheStore outputCache)
    {
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

        using var payload = new MemoryStream(data);
        using var container = new Container((CompressionType)request.CompressionType, payload, request.Version ?? -1);
        // A failed write may still have changed the store, as with raw store writes.
        try { cacheApi.Write(indexId, fileId, container); }
        finally
        {
            if (TextureEndpoints.IsImageSource(indexId))
                await outputCache.EvictByTagAsync(TextureEndpoints.ImageCacheTag, CancellationToken.None);
        }
        return TypedResults.Ok(new WriteContainerResultDto(
            Action: "write-container",
            IndexId: indexId,
            FileId: fileId));
    }

    private static GetContainerDto MapContainer(IContainer container)
    {
        var buffer = container.Data.ToArray();
        return new GetContainerDto(
            CompressionType: (byte)container.CompressionType,
            Version: container.Version,
            IsVersioned: container.IsVersioned(),
            DataBase64: Convert.ToBase64String(buffer),
            DataLength: buffer.Length);
    }
}
