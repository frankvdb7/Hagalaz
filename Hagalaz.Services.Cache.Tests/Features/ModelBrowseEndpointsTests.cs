using System.Net;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Services.Cache.Features.Models;
using Hagalaz.Services.Cache.Features.Models.Dtos;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public sealed class ModelBrowseEndpointsTests
{
    private static MemoryFileStore Store()
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 0, (ReferenceTableFlags)0, 3);
        foreach (var id in new[] { 0, 2, 666 })
        {
            var entry = new ReferenceTableEntry(id);
            entry.InitializeEntries(0);
            table.AddEntry(id, entry);
        }
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var container = new Container(new MemoryStream(encoded.ToArray()));
        store.Set(255, 7, container.Encode());
        return store;
    }

    [TestMethod]
    public async Task BrowseModels_UsesSparseMetadataWithoutDecodingGeometry()
    {
        var store = Store();
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var capacity = await client.GetFromJsonAsync<GetModelCapacityDto>("/api/v1/cache/models/capacity");
        Assert.AreEqual(667, capacity!.Capacity);
        var page = await client.GetFromJsonAsync<GetModelSummaryDto[]>("/api/v1/cache/models/range?startId=0&endIdExclusive=10");
        CollectionAssert.AreEqual(new[] {0,2}, page!.Select(model => model.Id).ToArray());
        var final = await client.GetFromJsonAsync<GetModelSummaryDto[]>("/api/v1/cache/models/range?startId=660&endIdExclusive=667");
        CollectionAssert.AreEqual(new[] {666}, final!.Select(model => model.Id).ToArray());
        Assert.AreEqual(0, store.GetReadCount(7, 0));
        Assert.AreEqual(0, store.GetReadCount(7, 666));
    }

    [TestMethod]
    [DataRow(-1, 10)]
    [DataRow(10, 9)]
    [DataRow(0, 201)]
    public async Task BrowseModels_RejectsInvalidRanges(int start, int end)
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(), group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync($"/api/v1/cache/models/range?startId={start}&endIdExclusive={end}");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
