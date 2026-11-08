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
        types.MapGet("/maps/floors", GetFloors).WithSummary("List every floor a terrain tile can store").Produces<IReadOnlyList<Hagalaz.Cache.Types.FloorRenderDefinition>>();
        types.MapPost("/maps/worldmap/refresh", RefreshWorldMap).WithSummary("Regenerate regions of the main world map from their map files").Produces<MapWorldMapRefreshResult>();
        types.MapPost("/maps/{id:int}/terrain", UpdateTerrain).WithSummary("Change individual terrain tiles").Produces<UpdateMapTerrainResultDto>();
        types.MapPost("/maps/{id:int}/landscape", ReplaceLandscape).WithSummary("Replace a region's landscape placements").Produces<UpdateMapLandscapeResultDto>();
        return types;
    }

    private static IResult RefreshWorldMap(RefreshWorldMapDto request, ICacheAPI cache, ITypeProvider<IObjectType> objects, CancellationToken cancellationToken)
    {
        if (request.Regions is not { Count: > 0 })
            return TypedResults.BadRequest(EndpointErrors.Validation("regions must name at least one region."));
        if (request.Regions.Any(id => id is < 0 or > 65535))
            return TypedResults.BadRequest(EndpointErrors.Validation("Region IDs must be between 0 and 65535."));
        return TypedResults.Ok(MapWorldMapRefresher.Create(cache, objects).Refresh(request.Regions, cancellationToken));
    }

    private static IResult GetFloors(ICacheAPI cache, CancellationToken cancellationToken) =>
        TypedResults.Ok(new MapRenderReader(cache).ReadFloorCatalog(cancellationToken));

    private static IResult UpdateTerrain(int id, UpdateMapTerrainDto request, ICacheAPI cache, CancellationToken cancellationToken)
    {
        if (id is < 0 or > 65535)
            return TypedResults.BadRequest(EndpointErrors.Validation("Region ID must be between 0 and 65535."));
        if (request.Tiles is null)
            return TypedResults.BadRequest(EndpointErrors.Validation("tiles is required."));
        if (request.Tiles.FirstOrDefault(tile => !IsValidTile(tile)) is { } invalid)
            return TypedResults.BadRequest(EndpointErrors.Validation($"Change to tile {invalid.X},{invalid.Y} on plane {invalid.Plane} is out of range."));

        return new MapTerrainWriter(cache).Apply(id, request.Tiles, cancellationToken) switch
        {
            MapArchiveUpdateResult.Absent => TypedResults.NotFound(EndpointErrors.Validation($"Region {id} has no terrain archive.", StatusCodes.Status404NotFound)),
            MapArchiveUpdateResult.Unreadable => TypedResults.Problem(title: "Terrain cannot be changed", detail: $"Region {id}'s stored terrain could not be decoded.", statusCode: StatusCodes.Status422UnprocessableEntity),
            _ => TypedResults.Ok(new UpdateMapTerrainResultDto("map-terrain-updated", id, request.Tiles.Count)),
        };
    }

    // A stored height of 1 is read as 0, so it has no meaning of its own.
    private static bool IsValidTile(Hagalaz.Cache.Types.MapTerrainTileChange tile) =>
        tile.Plane is >= 0 and <= 3
        && tile.X is >= 0 and <= 63
        && tile.Y is >= 0 and <= 63
        && tile.Height is null or (>= 0 and <= 255 and not 1)
        && tile.Underlay is null or (>= 0 and <= 174)
        && tile.Overlay is null or (>= 0 and <= 255)
        && tile.OverlayShape is null or (>= 0 and <= 11)
        && tile.OverlayRotation is null or (>= 0 and <= 3);

    private static IResult ReplaceLandscape(int id, UpdateMapLandscapeDto request, ICacheAPI cache, ITypeProvider<IObjectType> objects, CancellationToken cancellationToken)
    {
        if (id is < 0 or > 65535)
            return TypedResults.BadRequest(EndpointErrors.Validation("Region ID must be between 0 and 65535."));
        if (request.Objects is null)
            return TypedResults.BadRequest(EndpointErrors.Validation("objects is required."));
        var objectLimit = objects.ArchiveSize;
        if (request.Objects.FirstOrDefault(placement => !IsValidPlacement(placement, objectLimit)) is { } invalid)
            return TypedResults.BadRequest(EndpointErrors.Validation($"Placement of object {invalid.Id} at {invalid.X},{invalid.Y} on plane {invalid.Plane} is out of range."));

        return new MapLandscapeWriter(cache).Replace(id, request.Objects, cancellationToken) switch
        {
            MapArchiveUpdateResult.Absent => TypedResults.NotFound(EndpointErrors.Validation($"Region {id} has no landscape archive.", StatusCodes.Status404NotFound)),
            MapArchiveUpdateResult.Unreadable => TypedResults.Problem(title: "Landscape cannot be replaced", detail: $"Region {id}'s stored landscape is encrypted or corrupt, so saving would lose its placements.", statusCode: StatusCodes.Status422UnprocessableEntity),
            _ => TypedResults.Ok(new UpdateMapLandscapeResultDto("map-landscape-updated", id, request.Objects.Count)),
        };
    }

    private static bool IsValidPlacement(Hagalaz.Cache.Types.MapRenderPlacement placement, int objectLimit) =>
        placement.Id >= 0 && placement.Id < objectLimit
        && placement.Shape is >= 0 and <= 22
        && placement.Rotation is >= 0 and <= 3
        && placement.X is >= 0 and <= 63
        && placement.Y is >= 0 and <= 63
        && placement.Plane is >= 0 and <= 3;

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
