using System.Net;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features.RawFiles;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class RawFileEndpointDtoTests
{
    [TestMethod]
    public async Task RouteDtos_RetainBinaryFileAndMemberBytesAndDownloadNames()
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var entry = new ReferenceTableEntry(0);
        entry.InitializeEntries(1);
        entry.AddEntry(0, new ReferenceTableChildEntry(0));
        table.AddEntry(3, entry);
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var reference = new Container(new MemoryStream(encoded.ToArray()));
        using var container = new Container(new MemoryStream([1, 2, 3]));
        var raw = container.Encode();
        store.Set(255, 7, reference.Encode());
        store.Set(7, 3, raw);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapRawFileEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var file = await client.GetAsync("/api/v1/cache/indexes/7/files/3/raw");
        Assert.AreEqual(HttpStatusCode.OK, file.StatusCode);
        Assert.AreEqual("application/octet-stream", file.Content.Headers.ContentType!.MediaType);
        Assert.AreEqual("index-7-file-3.bin", file.Content.Headers.ContentDisposition!.FileNameStar);
        CollectionAssert.AreEqual(raw, await file.Content.ReadAsByteArrayAsync());
        using var member = await client.GetAsync("/api/v1/cache/indexes/7/files/3/sub-files/0/raw");
        Assert.AreEqual(HttpStatusCode.OK, member.StatusCode);
        Assert.AreEqual("application/octet-stream", member.Content.Headers.ContentType!.MediaType);
        Assert.AreEqual("index-7-file-3-sub-file-0.bin", member.Content.Headers.ContentDisposition!.FileNameStar);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, await member.Content.ReadAsByteArrayAsync());
    }

    [TestMethod]
    [DataRow("/indexes/7/files/3/raw")]
    [DataRow("/indexes/7/files/3/sub-files/0/raw")]
    public async Task RouteDtos_RetainMissingAssetResponses(string path)
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapRawFileEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync("/api/v1/cache" + path);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
