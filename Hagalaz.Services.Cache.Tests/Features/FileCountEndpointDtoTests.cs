using System.Net;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features.FileCounts;
using Hagalaz.Services.Cache.Features.FileCounts.Dtos;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class FileCountEndpointDtoTests
{
    [TestMethod]
    public async Task CountDtos_RetainSparseFileCountsMemberCountsAndMissingArchiveZero()
    {
        var store = new MemoryFileStore();
        store.Set(7, 3, []);
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var entry = new ReferenceTableEntry(3);
        entry.InitializeEntries(2);
        entry.AddEntry(1, new ReferenceTableChildEntry(0));
        entry.AddEntry(9, new ReferenceTableChildEntry(1));
        table.AddEntry(3, entry);
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var container = new Container(new MemoryStream(encoded.ToArray()));
        store.Set(255, 7, container.Encode());
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapFileCountEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var count = await client.GetFromJsonAsync<GetFileCountDto>("/api/v1/cache/indexes/7/file-count");
        Assert.AreEqual(new GetFileCountDto(4), count);
        var members = await client.GetFromJsonAsync<GetSubFileCountDto>("/api/v1/cache/indexes/7/files/3/sub-file-count");
        Assert.AreEqual(new GetSubFileCountDto(2), members);
        var missing = await client.GetFromJsonAsync<GetSubFileCountDto>("/api/v1/cache/indexes/7/files/4/sub-file-count");
        Assert.AreEqual(new GetSubFileCountDto(0), missing);
        Assert.AreEqual("{\"count\":0}", await client.GetStringAsync("/api/v1/cache/indexes/8/file-count"));
    }

    [TestMethod]
    [DataRow("/indexes/-1/file-count")]
    [DataRow("/indexes/-1/files/0/sub-file-count")]
    [DataRow("/indexes/7/files/-1/sub-file-count")]
    public async Task CountDtos_RetainNegativeIdValidation(string path)
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapFileCountEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync("/api/v1/cache" + path);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
