using System;
using System.Collections.Generic;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Services.Cache.Features.Checksums.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.Checksums;

public static class ChecksumEndpoints
{
    public static IEndpointRouteBuilder MapChecksumEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/checksum-table", GetChecksumTable);
        return group;
    }

    private static IResult GetChecksumTable(ICacheAPI cacheApi)
    {
        var table = cacheApi.CreateChecksumTable();
        return TypedResults.Ok(MapChecksumTable(table));
    }

    private static GetChecksumTableDto MapChecksumTable(IChecksumTable table)
    {
        var entries = new List<ChecksumTableEntryDto>(table.Count);
        for (var index = 0; index < table.Count; index++)
        {
            var entry = table[index];
            entries.Add(new ChecksumTableEntryDto(index, entry.Crc32, entry.Version, Convert.ToBase64String(entry.Digest)));
        }

        return new GetChecksumTableDto(entries);
    }
}
