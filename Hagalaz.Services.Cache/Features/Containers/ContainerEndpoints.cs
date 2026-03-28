using System;
using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features;

public static class ContainerEndpoints
{
    public static IEndpointRouteBuilder MapContainerEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/indexes/{indexId:int}/files/{fileId:int}/container", GetContainer);
        group.MapPost("/indexes/{indexId:int}/files/{fileId:int}/container/decrypt", GetContainerWithXtea);
        group.MapPost("/indexes/{indexId:int}/files/{fileId:int}/actions/write-container", WriteContainer);
        return group;
    }

    private static IResult GetContainer(int indexId, int fileId, ICacheAPI cacheApi)
    {
        try
        {
            using var container = cacheApi.ReadContainer(indexId, fileId);
            return TypedResults.Ok(MapContainer(container));
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    private static IResult GetContainerWithXtea(int indexId, int fileId, ReadContainerWithXteaRequest request, ICacheAPI cacheApi)
    {
        if (request.XteaKeys is null || request.XteaKeys.Length != 4)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("xteaKeys must contain exactly 4 integers."));
        }

        try
        {
            using var container = cacheApi.ReadContainer(indexId, fileId, request.XteaKeys);
            return TypedResults.Ok(MapContainer(container));
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    private static IResult WriteContainer(int indexId, int fileId, WriteContainerRequest request, ICacheAPI cacheApi)
    {
        if (string.IsNullOrWhiteSpace(request.DataBase64))
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("dataBase64 is required."));
        }

        try
        {
            var data = Convert.FromBase64String(request.DataBase64);
            using var payload = new MemoryStream(data);
            using var container = new Container(request.CompressionType, payload, request.Version ?? -1);
            cacheApi.Write(indexId, fileId, container);
            return TypedResults.Ok(new CacheMutationResponse(
                Action: "write-container",
                IndexId: indexId,
                FileId: fileId));
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

    private static ContainerResponse MapContainer(IContainer container)
    {
        var buffer = container.Data.ToArray();
        return new ContainerResponse(
            CompressionType: container.CompressionType,
            Version: container.Version,
            IsVersioned: container.IsVersioned(),
            DataBase64: Convert.ToBase64String(buffer),
            DataLength: buffer.Length);
    }

    public sealed record WriteContainerRequest(string DataBase64, CompressionType CompressionType = CompressionType.None, short? Version = null);

    public sealed record ReadContainerWithXteaRequest(int[] XteaKeys);

    public sealed record ContainerResponse(
        CompressionType CompressionType,
        short Version,
        bool IsVersioned,
        string DataBase64,
        int DataLength);

    public sealed record CacheMutationResponse(string Action, int IndexId, int FileId);
}
