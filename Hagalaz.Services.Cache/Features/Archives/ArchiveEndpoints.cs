using System;
using System.Collections.Generic;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features;

public static class ArchiveEndpoints
{
    public static IEndpointRouteBuilder MapArchiveEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/indexes/{indexId:int}/files/{fileId:int}/archive", GetArchive);
        return group;
    }

    private static IResult GetArchive(int indexId, int fileId, ICacheAPI cacheApi)
    {
        try
        {
            using var archive = cacheApi.ReadArchive(indexId, fileId);
            return TypedResults.Ok(MapArchive(fileId, archive));
        }
        catch (Exception ex)
        {
            return EndpointErrors.MapException(ex);
        }
    }

    private static ArchiveResponse MapArchive(int fileId, IArchive archive)
    {
        var results = new List<ArchiveEntryResponse>();

        if (archive.Entries is not null)
        {
            for (var i = 0; i < archive.Entries.Length; i++)
            {
                var data = archive.GetEntry(i).ToArray();
                results.Add(new ArchiveEntryResponse(i, Convert.ToBase64String(data), data.Length));
            }
        }

        return new ArchiveResponse(fileId, results);
    }

    public sealed record ArchiveResponse(int FileId, IReadOnlyList<ArchiveEntryResponse> Entries);

    public sealed record ArchiveEntryResponse(int SubFileId, string DataBase64, int DataLength);
}
