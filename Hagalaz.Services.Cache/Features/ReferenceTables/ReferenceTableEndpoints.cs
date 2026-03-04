using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features;

public static class ReferenceTableEndpoints
{
    public static IEndpointRouteBuilder MapReferenceTableEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/indexes/{indexId:int}/reference-table", GetReferenceTable);
        return group;
    }

    private static IResult GetReferenceTable(
        int indexId,
        ICacheAPI cacheApi)
    {
        try
        {
            var table = cacheApi.ReadReferenceTable(indexId);
            return TypedResults.Ok(MapReferenceTable(table));
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    private static ReferenceTableResponse MapReferenceTable(IReferenceTable table)
    {
        var entries = new List<ReferenceTableEntryResponse>(table.Entries.Count());

        foreach (var pair in table.Entries)
        {
            var entry = pair.Value;
            var children = entry.Entries
                .Select(child => new ReferenceTableChildEntryResponse(
                    ChildId: child.Key,
                    Id: child.Value.Id,
                    Index: child.Value.Index))
                .ToList();

            entries.Add(new ReferenceTableEntryResponse(
                FileId: pair.Key,
                Id: entry.Id,
                Crc32: entry.Crc32,
                Version: entry.Version,
                Capacity: entry.Capacity,
                WhirlpoolDigestBase64: Convert.ToBase64String(entry.WhirlpoolDigest),
                Children: children));
        }

        return new ReferenceTableResponse(
            Version: table.Version,
            Protocol: table.Protocol,
            Flags: table.Flags,
            Capacity: table.Capacity,
            Entries: entries);
    }

    public sealed record ReferenceTableResponse(
        int Version,
        byte Protocol,
        ReferenceTableFlags Flags,
        int Capacity,
        IReadOnlyList<ReferenceTableEntryResponse> Entries);

    public sealed record ReferenceTableEntryResponse(
        int FileId,
        int Id,
        int Crc32,
        int Version,
        int Capacity,
        string WhirlpoolDigestBase64,
        IReadOnlyList<ReferenceTableChildEntryResponse> Children);

    public sealed record ReferenceTableChildEntryResponse(int ChildId, int Id, int Index);
}
