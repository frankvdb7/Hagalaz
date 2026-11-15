using System.Net;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features.Archives;
using Hagalaz.Services.Cache.Features.Archives.Dtos;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class ArchiveEndpointDtoTests
{
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public async Task ReadArchive_RetainsMemberOrderBase64AndEmptyEntries(int memberCount)
    {
        var store = Seed(memberCount);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapArchiveEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var archive = await client.GetFromJsonAsync<GetArchiveDto>("/api/v1/cache/indexes/7/files/0/archive");
        Assert.IsNotNull(archive);
        Assert.AreEqual(0, archive.FileId);
        Assert.HasCount(memberCount, archive.Entries);
        Assert.AreEqual(new ArchiveEntryDto(0, "AwEE", 3), archive.Entries[0]);
        if (memberCount == 2)
            Assert.AreEqual(new ArchiveEntryDto(1, "", 0), archive.Entries[1]);
    }

    [TestMethod]
    public async Task ReadMissingArchive_RetainsNotFoundResponse()
    {
        await using var app = await CacheServiceTestHost.StartAsync(Seed(1), group => group.MapArchiveEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync("/api/v1/cache/indexes/7/files/1/archive");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static MemoryFileStore Seed(int memberCount)
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var entry = new ReferenceTableEntry(0);
        entry.InitializeEntries(memberCount);
        for (var i = 0; i < memberCount; i++) entry.AddEntry(i, new ReferenceTableChildEntry(i));
        table.AddEntry(0, entry);
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var reference = new Container(new MemoryStream(encoded.ToArray()));
        // Two-member single-chunk fixture: payload [3,1,4], empty payload, lengths 3 and 0.
        byte[] data = memberCount == 1 ? [3, 1, 4] : [3, 1, 4, 0, 0, 0, 3, 255, 255, 255, 253, 1];
        using var container = new Container(new MemoryStream(data));
        store.Set(255, 7, reference.Encode());
        store.Set(7, 0, container.Encode());
        return store;
    }
}
