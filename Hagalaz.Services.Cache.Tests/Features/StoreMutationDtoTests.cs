using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hagalaz.Services.Cache.Features.Mutations;
using Hagalaz.Services.Cache.Features.Mutations.Dtos;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class StoreMutationDtoTests
{
    [TestMethod]
    public async Task WriteAndCopyDtos_RetainResultFieldsAndExactBytes()
    {
        var store = new MemoryFileStore();
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapStoreMutationEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var write = await client.PostAsJsonAsync("/api/v1/cache/store/indexes/7/files/1/actions/write-raw", new WriteRawFileDto("AQID"));
        Assert.AreEqual(HttpStatusCode.OK, write.StatusCode);
        Assert.AreEqual(new WriteRawFileResultDto("write-raw", 7, 1), await write.Content.ReadFromJsonAsync<WriteRawFileResultDto>());
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, store.GetBytes(7, 1));
        using var json = JsonDocument.Parse(await write.Content.ReadAsStringAsync());
        CollectionAssert.AreEquivalent(new[] { "action", "indexId", "fileId", "sourceIndexId", "sourceFileId" },
            json.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.AreEqual(JsonValueKind.Null, json.RootElement.GetProperty("sourceIndexId").ValueKind);
        Assert.AreEqual(JsonValueKind.Null, json.RootElement.GetProperty("sourceFileId").ValueKind);
        using var copy = await client.PostAsJsonAsync("/api/v1/cache/store/indexes/8/files/2/actions/copy-raw", new CopyRawFileDto(7, 1));
        Assert.AreEqual(HttpStatusCode.OK, copy.StatusCode);
        Assert.AreEqual(new CopyRawFileResultDto("copy-raw", 8, 2, 7, 1), await copy.Content.ReadFromJsonAsync<CopyRawFileResultDto>());
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, store.GetBytes(8, 2));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, store.GetBytes(7, 1));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not base64")]
    public async Task WriteDto_InvalidBase64DoesNotChangeDestination(string value)
    {
        var store = new MemoryFileStore();
        store.Set(7, 1, [9]);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapStoreMutationEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/store/indexes/7/files/1/actions/write-raw", new WriteRawFileDto(value));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        CollectionAssert.AreEqual(new byte[] { 9 }, store.GetBytes(7, 1));
    }

    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(0, -1)]
    public async Task CopyDto_InvalidSourceDoesNotChangeDestination(int sourceIndex, int sourceFile)
    {
        var store = new MemoryFileStore();
        store.Set(7, 1, [9]);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapStoreMutationEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/store/indexes/7/files/1/actions/copy-raw", new CopyRawFileDto(sourceIndex, sourceFile));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        CollectionAssert.AreEqual(new byte[] { 9 }, store.GetBytes(7, 1));
    }
}
