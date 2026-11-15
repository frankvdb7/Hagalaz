using System.Net;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features.Containers;
using Hagalaz.Services.Cache.Features.Containers.Dtos;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class ContainerEndpointDtoTests
{
    private const string Path = "/api/v1/cache/indexes/7/files/0/container";

    [TestMethod]
    public async Task ReadAndDecrypt_UseTheSameContainerDtoShape()
    {
        var store = Seed();
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapContainerEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var read = await client.GetFromJsonAsync<GetContainerDto>(Path);
        Assert.IsNotNull(read);
        Assert.AreEqual((byte)0, read.CompressionType);
        Assert.AreEqual((short)17, read.Version);
        Assert.IsTrue(read.IsVersioned);
        Assert.AreEqual("AwEE", read.DataBase64);
        Assert.AreEqual(3, read.DataLength);
        using var response = await client.PostAsJsonAsync(Path + "/decrypt", new DecryptContainerDto([0, 0, 0, 0]));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var decrypted = await response.Content.ReadFromJsonAsync<GetContainerDto>();
        Assert.AreEqual(read, decrypted);
    }

    [TestMethod]
    [DataRow((byte)0)]
    [DataRow((byte)2)]
    public async Task Write_MapsNumericCompressionAndPersistsBase64Payload(byte compression)
    {
        var store = Seed();
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapContainerEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync(
            "/api/v1/cache/indexes/7/files/0/actions/write-container", new WriteContainerDto("CQgHBg==", compression, 23));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.AreEqual(new WriteContainerResultDto("write-container", 7, 0), await response.Content.ReadFromJsonAsync<WriteContainerResultDto>());
        var saved = await client.GetFromJsonAsync<GetContainerDto>(Path);
        Assert.IsNotNull(saved);
        Assert.AreEqual(compression, saved.CompressionType);
        Assert.AreEqual("CQgHBg==", saved.DataBase64);
        Assert.AreEqual(4, saved.DataLength);
    }

    [TestMethod]
    public async Task WriteDefaultsAndInvalidRequests_RetainExistingContract()
    {
        var store = Seed();
        var original = store.GetBytes(7, 0);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapContainerEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        foreach (var body in new[] { "", "invalid base64" })
        {
            using var invalid = await client.PostAsJsonAsync("/api/v1/cache/indexes/7/files/0/actions/write-container", new WriteContainerDto(body));
            Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        using var decrypt = await client.PostAsJsonAsync(Path + "/decrypt", new DecryptContainerDto([1, 2]));
        Assert.AreEqual(HttpStatusCode.BadRequest, decrypt.StatusCode);
        CollectionAssert.AreEqual(original, store.GetBytes(7, 0));
        using var write = await client.PostAsJsonAsync("/api/v1/cache/indexes/7/files/0/actions/write-container", new { dataBase64 = "AQI=" });
        Assert.AreEqual(HttpStatusCode.OK, write.StatusCode);
        var saved = await client.GetFromJsonAsync<GetContainerDto>(Path);
        Assert.IsNotNull(saved);
        Assert.AreEqual((byte)0, saved.CompressionType);
        Assert.AreEqual("AQI=", saved.DataBase64);
    }

    private static MemoryFileStore Seed()
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var entry = new ReferenceTableEntry(0);
        entry.InitializeEntries(1);
        entry.AddEntry(0, new ReferenceTableChildEntry(0));
        table.AddEntry(0, entry);
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var reference = new Container(new MemoryStream(encoded.ToArray()));
        using var container = new Container(CompressionType.None, new MemoryStream([3, 1, 4]), 17);
        store.Set(255, 7, reference.Encode());
        store.Set(7, 0, container.Encode());
        return store;
    }
}
