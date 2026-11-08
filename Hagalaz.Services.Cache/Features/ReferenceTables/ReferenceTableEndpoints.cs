using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Services.Cache.Features.ReferenceTables.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.ReferenceTables;

public static class ReferenceTableEndpoints
{
    public static IEndpointRouteBuilder MapReferenceTableEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/indexes/{indexId:int}/reference-table", GetReferenceTable)
            .WithSummary("Read an index's reference table").Produces<GetReferenceTableDto>();
        return group;
    }

    private static IResult GetReferenceTable(
        int indexId,
        ICacheAPI cacheApi)
    {
        var table = cacheApi.ReadReferenceTable(indexId);
        return TypedResults.Ok(MapReferenceTable(table));
    }

    private static GetReferenceTableDto MapReferenceTable(IReferenceTable table)
    {
        var entries = new List<ReferenceTableEntryDto>(table.Count);

        foreach (var pair in table.Entries)
        {
            var entry = pair.Value;
            var children = entry.Entries
                .Select(child => new ReferenceTableChildEntryDto(
                    ChildId: child.Key,
                    Id: child.Value.Id,
                    Index: child.Value.Index))
                .ToList();

            entries.Add(new ReferenceTableEntryDto(
                FileId: pair.Key,
                Id: entry.Id,
                Crc32: entry.Crc32,
                Version: entry.Version,
                Capacity: entry.Capacity,
                WhirlpoolDigestBase64: Convert.ToBase64String(entry.WhirlpoolDigest),
                Children: children));
        }

        return new GetReferenceTableDto(
            Version: table.Version,
            Protocol: table.Protocol,
            Flags: (byte)table.Flags,
            Capacity: table.Capacity,
            Entries: entries,
            Count: table.Count);
    }
}
