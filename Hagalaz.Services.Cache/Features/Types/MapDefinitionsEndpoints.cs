using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.Types;

public static class MapDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapMapDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/maps/{id:int}", GetMapType);
        types.MapPost("/maps/{id:int}/decode", DecodeMapType);
        return types;
    }

    private static IResult GetMapType(int id, IMapProvider provider) => TypedResults.Ok(MapMap(provider.Get(id)));

    private static IResult DecodeMapType(int id, DecodeMapDto request, IMapProvider provider)
    {
        if (request.XteaKeys is null || request.XteaKeys.Length != 4)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("xteaKeys must contain exactly 4 integers."));
        }

        return TypedResults.Ok(MapMap(provider.Get(id, request.XteaKeys)));
    }

    private static GetMapDefinitionDto MapMap(IMapType map) =>
        new(
            map.Id,
            map.TerrainData.GetLength(0),
            map.TerrainData.GetLength(1),
            map.TerrainData.GetLength(2),
            map.Objects.Count,
            map.Objects.Select(obj => new MapObjectDto(obj.Id, obj.ShapeType, obj.Rotation, obj.X, obj.Y, obj.Z)).ToArray());
}
