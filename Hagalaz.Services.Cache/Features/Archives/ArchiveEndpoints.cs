using System;
using System.Collections.Generic;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Services.Cache.Features.Archives.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.Archives;

public static class ArchiveEndpoints
{
    public static IEndpointRouteBuilder MapArchiveEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/indexes/{indexId:int}/files/{fileId:int}/archive", GetArchive);
        return group;
    }

    private static IResult GetArchive(int indexId, int fileId, ICacheAPI cacheApi)
    {
        using var archive = cacheApi.ReadArchive(indexId, fileId);
        return TypedResults.Ok(MapArchive(fileId, archive));
    }

    private static GetArchiveDto MapArchive(int fileId, IArchive archive)
    {
        var results = new List<ArchiveEntryDto>();

        if (archive.Entries is not null)
        {
            for (var i = 0; i < archive.Entries.Length; i++)
            {
                var data = archive.GetEntry(i).ToArray();
                results.Add(new ArchiveEntryDto(i, Convert.ToBase64String(data), data.Length));
            }
        }

        return new GetArchiveDto(fileId, results);
    }
}
