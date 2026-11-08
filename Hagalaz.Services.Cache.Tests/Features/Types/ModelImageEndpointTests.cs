using System.Net;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features.Models;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public sealed class ModelImageEndpointTests
{
    // The legacy single-triangle model used by the item icon tests.
    private static readonly byte[] TriangleModel = [0, 1, 3, 1, 2, 64, 65, 65, 0, 42, 0, 0, 0, 1, 0, 2, 74, 54, 84,
        0, 3, 0, 1, 1, 1, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3];

    [TestMethod]
    public async Task Image_RendersAnyModelAtTheRequestedSizeAndRevalidates()
    {
        await using var app = await StartAsync();
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.GetAsync("/api/v1/cache/models/0/image?width=64&height=48");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.AreEqual("image/png", response.Content.Headers.ContentType?.MediaType);
        using var image = Image.Load<Rgba32>(await response.Content.ReadAsByteArrayAsync());
        Assert.AreEqual((64, 48), (image.Width, image.Height));

        using var conditional = new HttpRequestMessage(HttpMethod.Get, "/api/v1/cache/models/0/image?width=64&height=48");
        conditional.Headers.IfNoneMatch.Add(response.Headers.ETag!);
        using var notModified = await client.SendAsync(conditional);
        Assert.AreEqual(HttpStatusCode.NotModified, notModified.StatusCode);

        using var other = await client.GetAsync("/api/v1/cache/models/0/image");
        Assert.AreNotEqual(response.Headers.ETag, other.Headers.ETag);
    }

    [TestMethod]
    [DataRow("/api/v1/cache/models/-1/image", HttpStatusCode.BadRequest)]
    [DataRow("/api/v1/cache/models/0/image?width=7", HttpStatusCode.BadRequest)]
    [DataRow("/api/v1/cache/models/0/image?height=513", HttpStatusCode.BadRequest)]
    [DataRow("/api/v1/cache/models/9/image", HttpStatusCode.NotFound)]
    public async Task Image_RejectsInvalidRequestsAndMissingModels(string url, HttpStatusCode expected)
    {
        await using var app = await StartAsync();
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync(url);
        Assert.AreEqual(expected, response.StatusCode);
    }

    private static async Task<Microsoft.AspNetCore.Builder.WebApplication> StartAsync()
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var entry = new ReferenceTableEntry(0);
        entry.InitializeEntries(1);
        entry.AddEntry(0, new ReferenceTableChildEntry(0));
        table.AddEntry(0, entry);
        using var encoded = new ReferenceTableCodec().Encode(table);
        store.Set(255, 7, ContainerBytes(encoded.ToArray()));
        store.Set(7, 0, ContainerBytes(TriangleModel));
        return await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
    }

    private static byte[] ContainerBytes(byte[] bytes)
    {
        using var container = new Container(new MemoryStream(bytes));
        return container.Encode();
    }
}
