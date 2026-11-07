using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;
using static Hagalaz.Services.Cache.Features.Types.TypeMutationUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class ObjectDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapObjectDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/objects/{id:int}", GetObjectType);
        types.MapGet("/objects/range", GetObjectTypeRange);
        types.MapGet("/objects/catalog", GetObjectCatalog);
        types.MapPost("/objects/{id:int}/name", SetObjectName);
        return types;
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

    /// <summary>Drops name indexes after writes that keep the reference table, such as raw store writes.</summary>
    internal static void InvalidateCatalog() => CatalogNames.Clear();

    private static IResult GetObjectCatalog(ICacheAPI cache, ITypeProvider<IObjectType> provider, CancellationToken cancellationToken, string query = "", int offset = 0, int limit = 40)
    {
        if (ValidateSearch(query, offset, limit, provider.ArchiveSize, requireQuery: false) is { } invalid) return invalid;

        var data = new ObjectTypeData();
        var table = cache.ReadReferenceTable(data.IndexId);
        var ids = ObjectIds(table, data);
        query = query.Trim();
        var exactId = int.TryParse(query, out var requestedId);
        if (query.Length == 0 || exactId)
        {
            int[] matches = query.Length == 0 ? ids : Array.BinarySearch(ids, requestedId) >= 0 ? [requestedId] : [];
            var page = matches.Skip(offset).Take(limit).Select(id => CatalogEntry(provider.Get(id))).ToArray();
            return TypedResults.Ok(new SearchDefinitionsDto<GetObjectCatalogEntryDto>(query, matches.Length, offset, limit, page));
        }

        if (!CatalogNames.TryGetValue(table, out var catalog))
        {
            catalog = BuildCatalog(ids, data.ArchiveEntrySize, provider, cancellationToken);
            CatalogNames.AddOrUpdate(table, catalog);
        }
        return CreateSearchResponse(query, offset, limit, catalog.Where(entry => entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray());
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
        IObjectTypeCodec codec,
        ICacheAPI cacheApi)
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

        var typeData = new ObjectTypeData();
        var obj = DecodeRawTypeEntry(id, codec, typeData, cacheApi);
        obj.Name = request.Name;
        PersistTypeEntry(id, obj, codec, typeData, cacheApi);
        return TypedResults.Ok(new UpdateObjectNameResultDto("object-name-updated", "object", id));
    }
}
