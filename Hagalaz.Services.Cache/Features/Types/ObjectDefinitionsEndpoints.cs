using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class ObjectDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapObjectDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/objects/{id:int}", GetObjectType).WithSummary("Get an object definition").Produces<GetObjectDefinitionDto>();
        types.MapGet("/objects/range", GetObjectTypeRange).WithSummary("List object definitions in a range").Produces<GetObjectDefinitionDto[]>();
        types.MapGet("/objects/catalog", GetObjectCatalog).WithSummary("Search the object catalog by name or ID").Produces<SearchDefinitionsDto<GetObjectCatalogEntryDto>>();
        types.MapPost("/objects/{id:int}/name", SetObjectName).WithSummary("Rename an object").Produces<UpdateObjectNameResultDto>();
        types.MapPost("/objects/collision", GetObjectCollision)
            .WithSummary("Read collision fields for 1 to 256 placed object definition IDs")
            .Produces<GetObjectCollisionResultDto[]>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
        return types;
    }

    private static IResult GetObjectCollision(
        GetObjectCollisionDto request, ICacheAPI cache, ITypeProvider<IObjectType> provider, CancellationToken cancellationToken)
    {
        if (request.Ids is not { Length: >= 1 and <= 256 } ids)
            return TypedResults.BadRequest(EndpointErrors.Validation("ids must contain 1 to 256 object definition IDs."));

        var data = new ObjectTypeData();
        var table = cache.ReadReferenceTable(data.IndexId);
        var results = new GetObjectCollisionResultDto[ids.Length];
        for (var i = 0; i < ids.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = ids[i];
            if (id < 0 || table.GetEntry(data.GetArchiveId(id), data.GetArchiveEntryId(id)) is null)
            {
                results[i] = new(id, "not-found", null);
                continue;
            }

            try
            {
                var definition = provider.Get(id);
                results[i] = new(id, "ok", new(definition.SizeX, definition.SizeY, definition.ClipType, definition.Solid, definition.Gateway));
            }
            catch (Exception error) when (error is IOException or InvalidDataException or NotSupportedException)
            {
                results[i] = new(id, "unavailable", null);
            }
        }

        return TypedResults.Ok(results);
    }

    private static IResult GetObjectType(int id, ITypeProvider<IObjectType> provider) => TypedResults.Ok(MapObject(provider.Get(id)));

    private static IResult GetObjectTypeRange(int startId, int endIdExclusive, ITypeProvider<IObjectType> provider)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        return error ?? TypedResults.Ok(provider.GetRange(startId, endIdExclusive).Select(MapObject).ToArray());
    }

    private static GetObjectDefinitionDto MapObject(IObjectType obj) =>
        new(obj.Id, obj.Name, obj.SizeX, obj.SizeY, obj.Solid, obj.Gateway, obj.ClipType, obj.Surroundings, obj.Actions, obj.VarpBitFileId);

    // Keyed by the cache's reference-table snapshot, which is replaced whenever the object index is written.
    private static readonly ConditionalWeakTable<IReferenceTable, GetObjectCatalogEntryDto[]> CatalogNames = new();

    private static readonly Lock CatalogLock = new();
    private static int _catalogGeneration;

    /// <summary>Drops name indexes after writes that keep the reference table, such as raw store writes.</summary>
    internal static void InvalidateCatalog()
    {
        lock (CatalogLock)
        {
            // Builds that started before this write must not store their stale result.
            _catalogGeneration++;
            CatalogNames.Clear();
        }
    }

    private static IResult GetObjectCatalog(ICacheAPI cache, ITypeProvider<IObjectType> provider, CancellationToken cancellationToken, string query = "", int offset = 0, int limit = 40, string? shapes = null)
    {
        if (ValidateSearch(query, offset, limit, provider.ArchiveSize, requireQuery: false) is { } invalid) return invalid;
        if (!TryParseShapes(shapes, out var shapeFilter))
            return TypedResults.BadRequest(EndpointErrors.Validation("shapes must be a comma-separated list of object shapes from 0 to 22."));

        var data = new ObjectTypeData();
        var table = cache.ReadReferenceTable(data.IndexId);
        var ids = ObjectIds(table, data);
        query = query.Trim();
        var exactId = int.TryParse(query, out var requestedId);
        if (shapeFilter is null && (query.Length == 0 || exactId))
        {
            int[] matches = query.Length == 0 ? ids : Array.BinarySearch(ids, requestedId) >= 0 ? [requestedId] : [];
            var page = matches.Skip(offset).Take(limit).Select(id => CatalogEntry(provider.Get(id))).ToArray();
            return TypedResults.Ok(new SearchDefinitionsDto<GetObjectCatalogEntryDto>(query, matches.Length, offset, limit, page));
        }

        var filtered = LoadCatalog(table, ids, data, provider, cancellationToken).Where(entry =>
            (shapeFilter is null || (entry.Shape is { } shape && shapeFilter.Contains(shape)))
            && (query.Length == 0 || (exactId ? entry.Id == requestedId : entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase))));
        return CreateSearchResponse(query, offset, limit, filtered.ToArray());
    }

    private static bool TryParseShapes(string? value, out HashSet<int>? shapes)
    {
        shapes = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        var parsed = new HashSet<int>();
        foreach (var part in value.Split(','))
        {
            if (!int.TryParse(part, out var shape) || shape is < 0 or > 22) return false;
            parsed.Add(shape);
        }
        shapes = parsed;
        return true;
    }

    private static GetObjectCatalogEntryDto[] LoadCatalog(IReferenceTable table, int[] ids, ObjectTypeData data, ITypeProvider<IObjectType> provider, CancellationToken cancellationToken)
    {
        if (CatalogNames.TryGetValue(table, out var catalog)) return catalog;
        var generation = Volatile.Read(ref _catalogGeneration);
        catalog = BuildCatalog(ids, data.ArchiveEntrySize, provider, cancellationToken);
        lock (CatalogLock)
        {
            if (generation == _catalogGeneration) CatalogNames.AddOrUpdate(table, catalog);
        }
        return catalog;
    }

    private static int[] ObjectIds(IReferenceTable table, ObjectTypeData data) => table.Entries
        .SelectMany(archive => archive.Value.Entries.Select(entry => archive.Key * data.ArchiveEntrySize + entry.Key))
        .Order().ToArray();

    private static GetObjectCatalogEntryDto[] BuildCatalog(int[] ids, int archiveEntrySize, ITypeProvider<IObjectType> provider, CancellationToken cancellationToken)
    {
        var entries = ids.ToHashSet();
        var catalog = new List<GetObjectCatalogEntryDto>(ids.Length);
        // Decode one existing archive at a time; absent archives cannot be read.
        foreach (var archive in ids.GroupBy(id => id / archiveEntrySize))
        {
            cancellationToken.ThrowIfCancellationRequested();
            catalog.AddRange(provider.GetRange(archive.First(), archive.Last() + 1).Where(definition => entries.Contains(definition.Id)).Select(CatalogEntry));
        }
        return catalog.ToArray();
    }

    private static GetObjectCatalogEntryDto CatalogEntry(IObjectType value)
    {
        var definition = (ObjectType)value;
        var shapes = (definition.Shapes ?? []).Take(definition.ModelIDs.Length).Where(shape => shape is >= 0 and <= 22).ToArray();
        return new(definition.Id, definition.Name, shapes.Contains((sbyte)10) ? 10 : shapes.Length > 0 ? shapes[0] : null);
    }

    private static IResult SetObjectName(
        int id,
        UpdateObjectNameDto request,
        TypeMutator<IObjectType> objects)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("name is required."));
        }

        for (var i = 0; i < request.Name.Length; i++)
        {
            if (request.Name[i] == '\0')
            {
                return TypedResults.BadRequest(EndpointErrors.Validation("name must not contain a null character."));
            }

            if (request.Name[i] > byte.MaxValue)
            {
                return TypedResults.BadRequest(EndpointErrors.Validation("name must contain only Latin-1 characters."));
            }
        }

        if (id < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("id must be greater than or equal to 0."));
        }

        var result = objects.Mutate(id, obj => obj.Name = request.Name);
        if (result.IsFailed) return EndpointErrors.TypeMutationFailure(result, "object", id);
        return TypedResults.Ok(new UpdateObjectNameResultDto("object-name-updated", "object", id));
    }
}
