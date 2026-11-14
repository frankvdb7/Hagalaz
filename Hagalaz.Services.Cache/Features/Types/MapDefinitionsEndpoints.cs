using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Maps;
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

    private static IResult RefreshWorldMap(RefreshWorldMapDto request, ICacheAPI cache, IRegionMapArchive maps, ITypeProvider<IObjectType> objects, CancellationToken cancellationToken)
    {
        if (request.Regions is not { Count: > 0 })
            return TypedResults.BadRequest(EndpointErrors.Validation("regions must name at least one region."));
        if (request.Regions.Any(id => id is < 0 or > 65535))
            return TypedResults.BadRequest(EndpointErrors.Validation("Region IDs must be between 0 and 65535."));
        return TypedResults.Ok(MapWorldMapRefresher.Create(cache, maps, objects).Refresh(request.Regions, cancellationToken));
    }

    private static IResult GetFloors(ICacheAPI cache, IRegionMapArchive maps, CancellationToken cancellationToken) =>
        TypedResults.Ok(new MapRenderReader(cache, maps).ReadFloorCatalog(cancellationToken));

    private static IResult UpdateTerrain(int id, UpdateMapTerrainDto request, IRegionMapArchive maps, CancellationToken cancellationToken)
    {
        if (id is < 0 or > 65535)
            return TypedResults.BadRequest(EndpointErrors.Validation("Region ID must be between 0 and 65535."));
        if (request.Tiles is null)
            return TypedResults.BadRequest(EndpointErrors.Validation("tiles is required."));
        if (request.Tiles.FirstOrDefault(tile => tile.Flags is { } flags && !MapTerrainPatcher.IsStorableFlags(flags)) is { } unstorable)
            return TypedResults.BadRequest(EndpointErrors.Validation($"Tile {unstorable.X},{unstorable.Y} on plane {unstorable.Plane} has flag value {unstorable.Flags}; a tile flag value must be between 0 and 32."));
        if (request.Tiles.FirstOrDefault(tile => !IsValidTile(tile)) is { } invalid)
            return TypedResults.BadRequest(EndpointErrors.Validation($"Change to tile {invalid.X},{invalid.Y} on plane {invalid.Plane} is out of range."));

        var result = maps.ApplyTerrain(id, request.Tiles, cancellationToken);
        return result.IsFailed ? EndpointErrors.RegionMapFileFailure(result, id) : TypedResults.Ok(new UpdateMapTerrainResultDto("map-terrain-updated", id, request.Tiles.Count));
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

    private static IResult ReplaceLandscape(int id, UpdateMapLandscapeDto request, IRegionMapArchive maps, ITypeProvider<IObjectType> objects, CancellationToken cancellationToken)
    {
        if (id is < 0 or > 65535)
            return TypedResults.BadRequest(EndpointErrors.Validation("Region ID must be between 0 and 65535."));
        if (request.Objects is null)
            return TypedResults.BadRequest(EndpointErrors.Validation("objects is required."));
        var objectLimit = objects.ArchiveSize;
        if (request.Objects.FirstOrDefault(placement => !IsValidPlacement(placement, objectLimit)) is { } invalid)
            return TypedResults.BadRequest(EndpointErrors.Validation($"Placement of object {invalid.Id} at {invalid.X},{invalid.Y} on plane {invalid.Plane} is out of range."));

        var result = maps.ReplaceLandscape(id, request.Objects, cancellationToken);
        return result.IsFailed ? EndpointErrors.RegionMapFileFailure(result, id) : TypedResults.Ok(new UpdateMapLandscapeResultDto("map-landscape-updated", id, request.Objects.Count));
    }

    private static bool IsValidPlacement(Hagalaz.Cache.Types.MapRenderPlacement placement, int objectLimit) =>
        placement.Id >= 0 && placement.Id < objectLimit
        && placement.Shape is >= 0 and <= 22
        && placement.Rotation is >= 0 and <= 3
        && placement.X is >= 0 and <= 63
        && placement.Y is >= 0 and <= 63
        && placement.Plane is >= 0 and <= 3;

    private static IResult GetRenderRegion(int id, MapRenderRequestDto request, ICacheAPI cache, IRegionMapArchive maps, CancellationToken cancellationToken)
    {
        if (id is < 0 or > 65535)
            return TypedResults.BadRequest(EndpointErrors.Validation("Region ID must be between 0 and 65535."));
        if (request.XteaKeys is not null && request.XteaKeys.Length != 4)
            return TypedResults.BadRequest(EndpointErrors.Validation("xteaKeys must contain exactly 4 integers."));
        return TypedResults.Ok(new MapRenderReader(cache, maps).Read(id, request.XteaKeys, cancellationToken));
    }

    private static IResult GetRegions(IRegionMapArchive maps, CancellationToken cancellationToken, int offset = 0, int limit = 10)
    {
        if (offset < 0 || offset > 65536 || limit < 1 || limit > 200)
            return TypedResults.BadRequest(EndpointErrors.Validation("offset must be between 0 and 65536; limit must be between 1 and 200."));

        var regions = maps.ListRegions(cancellationToken);
        if (regions.IsFailed)
            return TypedResults.Problem(title: "Region listing unavailable", detail: "The map index has no named reference metadata.", statusCode: 422);

        MapRegionSummaryDto[] page = [.. regions.Value.Skip(offset).Take(limit).Select(region => new MapRegionSummaryDto(region.RegionId, region.X, region.Y, region.TerrainFile, region.LandscapeFile))];
        return TypedResults.Ok(new GetMapRegionsDto(page, regions.Value.Count, offset, limit));
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
