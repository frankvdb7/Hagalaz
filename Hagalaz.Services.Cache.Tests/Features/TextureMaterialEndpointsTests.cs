using System.Net.Http.Json;
using System.Net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public sealed class TextureMaterialEndpointsTests
{
    [TestMethod]
    public async Task MaterialRequest_ReturnsAppearanceAndUnverifiedEffectMetadata()
    {
        var store = CreateStore();
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTextureEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        var result = await client.GetFromJsonAsync<MaterialDefinitionDecoder.Material>("/api/v1/cache/textures/0/material");

        Assert.IsNotNull(result);
        Assert.AreEqual(new MaterialDefinitionDecoder.Material(1, 170, 25, 2, 12, -1, 4, true, false, true, 0x12345678), result);
    }

    [TestMethod]
    public async Task HdrRequest_ReturnsFloatIntensitiesAndKeepsPngCompatible()
    {
        var store = CreateStore();
        AddImage(store, 9, 1, new Rgba32(255, 128, 0, 255));
        AddImage(store, 37, 1, new Rgba32(0, 8, 0, 255));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTextureEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var result = await client.GetFromJsonAsync<TextureImageDecoder.HdrImage>("/api/v1/cache/textures/0/hdr");
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Width); Assert.AreEqual(1, result.Height);
        CollectionAssert.AreEqual(new float[] {16.5f, 128/255f*16.5f, 0, 1}, result.Rgba);
        using var png = await client.GetAsync("/api/v1/cache/textures/0/image");
        Assert.AreEqual(HttpStatusCode.OK, png.StatusCode);
        Assert.AreEqual("image/png", png.Content.Headers.ContentType!.MediaType);
        using var decoded = Image.Load<Rgba32>(await png.Content.ReadAsByteArrayAsync());
        Assert.AreEqual(new Rgba32(255, 128, 0, 255), decoded[0,0]);
    }

    [TestMethod]
    [DataRow(false, HttpStatusCode.NotFound)]
    [DataRow(true, HttpStatusCode.UnprocessableEntity)]
    public async Task HdrRequest_ReportsMissingOrMismatchedCompanion(bool mismatch, HttpStatusCode expected)
    {
        var store = CreateStore();
        AddImage(store, 9, 1, new Rgba32(255, 255, 255));
        if (mismatch) AddImage(store, 37, 2, new Rgba32(0, 8, 0));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTextureEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync("/api/v1/cache/textures/0/hdr");
        Assert.AreEqual(expected, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static void AddImage(MemoryFileStore store, int index, int width, Rgba32 pixel)
    {
        using var image = new Image<Rgba32>(width, 1, pixel);
        using var bytes = new MemoryStream(); image.SaveAsPng(bytes);
        using var container = new Container(new MemoryStream(bytes.ToArray()));
        store.Set(index, 0, container.Encode());
    }

    private static MemoryFileStore CreateStore()
    {
        var store = new MemoryFileStore();
        var data = new byte[26];
        data[1] = 1; data[2] = 1;
        data[3 + 3] = 25; data[3 + 4] = 170;
        data[3 + 5] = 2; data[3 + 6] = 12;
        data[3 + 9] = 255; data[3 + 10] = 4;
        data[3 + 14] = 1; data[3 + 16] = 1;
        data[^1] = 1;
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(21, 4), 0x12345678);
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var archive = new ReferenceTableEntry(0);
        archive.InitializeEntries(1); archive.AddEntry(0, new ReferenceTableChildEntry(0)); table.AddEntry(0, archive);
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var reference = new Container(new MemoryStream(encoded.ToArray()));
        using var materialData = new Container(new MemoryStream(data));
        store.Set(255, 26, reference.Encode()); store.Set(26, 0, materialData.Encode());
        return store;
    }
}
