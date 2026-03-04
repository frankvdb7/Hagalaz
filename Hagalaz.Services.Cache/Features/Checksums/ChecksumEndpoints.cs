using System;
using System.Collections.Generic;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features;

public static class ChecksumEndpoints
{
    public static IEndpointRouteBuilder MapChecksumEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/checksum-table", GetChecksumTable);
        return group;
    }

    private static IResult GetChecksumTable(ICacheAPI cacheApi)
    {
        try
        {
            var table = cacheApi.CreateChecksumTable();
            return TypedResults.Ok(MapChecksumTable(table));
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    private static ChecksumTableResponse MapChecksumTable(IChecksumTable table)
    {
        var entries = new List<ChecksumTableEntryResponse>(table.Count);
        for (var index = 0; index < table.Count; index++)
        {
            var entry = table[index];
            entries.Add(new ChecksumTableEntryResponse(index, entry.Crc32, entry.Version, Convert.ToBase64String(entry.Digest)));
        }

        return new ChecksumTableResponse(entries);
    }

    public sealed record ChecksumTableResponse(IReadOnlyList<ChecksumTableEntryResponse> Entries);

    public sealed record ChecksumTableEntryResponse(int IndexId, int Crc32, int Version, string DigestBase64);
}
