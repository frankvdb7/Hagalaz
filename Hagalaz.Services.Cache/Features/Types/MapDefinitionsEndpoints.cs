using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types.Providers;
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
        types.MapGet("/maps", GetRegions).WithSummary("List named map regions").Produces<GetMapRegionsDto>();
        types.MapGet("/maps/{id:int}", GetMapType).WithSummary("Decode a map region without keys").Produces<GetMapDefinitionDto>();
        types.MapPost("/maps/{id:int}/decode", DecodeMapType).WithSummary("Decode a map region with XTEA keys").Produces<GetMapDefinitionDto>();
        types.MapPost("/maps/{id:int}/render", GetRenderRegion).WithSummary("Read a region's terrain, placements and floors for rendering").Produces<Hagalaz.Cache.Types.MapRenderRegion>();
        return types;
    }

    private static IResult GetRenderRegion(int id, MapRenderRequestDto request, ICacheAPI cache, CancellationToken cancellationToken)
    {
        if (id is < 0 or > 65535)
            return TypedResults.BadRequest(EndpointErrors.Validation("Region ID must be between 0 and 65535."));
        if (request.XteaKeys is not null && request.XteaKeys.Length != 4)
            return TypedResults.BadRequest(EndpointErrors.Validation("xteaKeys must contain exactly 4 integers."));
        return TypedResults.Ok(new MapRenderReader(cache).Read(id, request.XteaKeys, cancellationToken));
    }

    private static IResult GetRegions(ICacheAPI cache, CancellationToken cancellationToken, int offset = 0, int limit = 10)
    {
        if (offset < 0 || offset > 65536 || limit < 1 || limit > 200)
            return TypedResults.BadRequest(EndpointErrors.Validation("offset must be between 0 and 65536; limit must be between 1 and 200."));

        var table = cache.ReadReferenceTable(5);
        if ((table.Flags & ReferenceTableFlags.Identifiers) == 0)
            return TypedResults.Problem(title: "Region listing unavailable", detail: "The map index has no named reference metadata.", statusCode: 422);

        // Reference-table entry IDs are filename hashes, not packed region IDs.
        var files = new Dictionary<int, int>();
        foreach (var (fileId, entry) in table.Entries) files.TryAdd(entry.Id, fileId);
        var regions = new List<MapRegionSummaryDto>();
        for (var x = 0; x < 256; x++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var y = 0; y < 256; y++)
            {
                var terrain = files.TryGetValue(ReferenceTable.CalculateId($"m{x}_{y}"), out var terrainId);
                var landscape = files.TryGetValue(ReferenceTable.CalculateId($"l{x}_{y}"), out var landscapeId);
                if (terrain || landscape)
                    regions.Add(new MapRegionSummaryDto((x << 8) | y, x, y, terrain ? terrainId : null, landscape ? landscapeId : null));
            }
        }
        return TypedResults.Ok(new GetMapRegionsDto(regions.Skip(offset).Take(limit).ToArray(), regions.Count, offset, limit));
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
