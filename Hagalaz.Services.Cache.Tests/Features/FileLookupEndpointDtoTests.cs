using System.Net;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features.FileLookup;
using Hagalaz.Services.Cache.Features.FileLookup.Dtos;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class FileLookupEndpointDtoTests
{
    [TestMethod]
    [DataRow("test_file", 3)]
    [DataRow("missing", -1)]
    public async Task LookupDto_RetainsFoundAndMissingNameResults(string name, int expectedId)
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 1, ReferenceTableFlags.Identifiers, 1);
        var entry = new ReferenceTableEntry(0) { Id = ReferenceTable.CalculateId("test_file") };
        entry.InitializeEntries(1);
        entry.AddEntry(0, new ReferenceTableChildEntry(0));
        table.AddEntry(3, entry);
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var container = new Container(new MemoryStream(encoded.ToArray()));
        store.Set(255, 7, container.Encode());
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapFileLookupEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync("/api/v1/cache/indexes/7/files/id?fileName=" + name);
        response.EnsureSuccessStatusCode();
        Assert.AreEqual(new GetFileIdDto(expectedId), await response.Content.ReadFromJsonAsync<GetFileIdDto>());
        Assert.AreEqual("{\"fileId\":" + expectedId + "}", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    [DataRow("/indexes/-1/files/id?fileName=test_file")]
    [DataRow("/indexes/7/files/id?fileName=%20")]
    [DataRow("/indexes/7/files/id")]
    public async Task LookupDto_RetainsInvalidIdAndQueryValidation(string path)
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapFileLookupEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync("/api/v1/cache" + path);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
