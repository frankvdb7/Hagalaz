using Hagalaz.Services.Cache.Features.Containers;
using Hagalaz.Services.Cache.Features.Containers.Dtos;
using Hagalaz.Services.Cache.Features.Mutations;
using Hagalaz.Services.Cache.Features.Mutations.Dtos;
using Hagalaz.Services.Cache.Features.Models.Dtos;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features;
using Hagalaz.Services.Cache.Features.Models;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;

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

        var result = await client.GetFromJsonAsync<GetTextureMaterialDto>("/api/v1/cache/textures/0/material");

        Assert.IsNotNull(result);
        Assert.AreEqual(new GetTextureMaterialDto(1, 170, 25, 2, 12, -1, 4, true, false, true, 0x12345678), result);
    }

    [TestMethod]
    [DataRow((byte)1, (byte)40)]
    [DataRow((byte)2, (byte)255)]
    public async Task ImageRequest_SourceAlphaKeepsOnlyTheEffectOneHighlightMask(byte effectId, byte expectedAlpha)
    {
        var store = CreateStore(effectId, alphaMode: 0);
        AddImage(store, 9, 1, new Rgba32(10, 20, 30, 40));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTextureEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var plain = await client.GetAsync("/api/v1/cache/textures/0/image");
        using var scene = await client.GetAsync("/api/v1/cache/textures/0/image?sourceAlpha=true");

        using var plainImage = Image.Load<Rgba32>(await plain.Content.ReadAsByteArrayAsync());
        using var sceneImage = Image.Load<Rgba32>(await scene.Content.ReadAsByteArrayAsync());
        Assert.AreEqual((byte)255, plainImage[0, 0].A);
        Assert.AreEqual(expectedAlpha, sceneImage[0, 0].A);
        Assert.AreEqual(new Rgba32(10, 20, 30, expectedAlpha), sceneImage[0, 0]);
        Assert.AreEqual(effectId == 1, plain.Headers.ETag!.Tag != scene.Headers.ETag!.Tag);
    }

    [TestMethod]
    public async Task ImageRequest_IncludesCompleteMaterialWithoutChangingPngOrLegacyHeaders()
    {
        var store = CreateStore();
        AddImage(store, 9, 2, new Rgba32(0, 0, 0, 255));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTextureEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.GetAsync("/api/v1/cache/textures/0/image");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("image/png", response.Content.Headers.ContentType!.MediaType);
        var header = response.Headers.GetValues("X-Texture-Material").Single();
        using var json = JsonDocument.Parse(header);
        Assert.AreEqual(0x12345678, json.RootElement.GetProperty("effectParameter2").GetInt32());
        var material = JsonSerializer.Deserialize<GetTextureMaterialDto>(header, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var standalone = await client.GetFromJsonAsync<GetTextureMaterialDto>("/api/v1/cache/textures/0/material");
        Assert.AreEqual(new GetTextureMaterialDto(1, 170, 25, 2, 12, -1, 4, true, false, true, 0x12345678), material);
        Assert.AreEqual(standalone, material);
        Assert.AreEqual("1", response.Headers.GetValues("X-Texture-Alpha-Mode").Single());
        Assert.AreEqual("170", response.Headers.GetValues("X-Texture-Color-Blend").Single());
        Assert.AreEqual("25", response.Headers.GetValues("X-Texture-Brightness").Single());
        using var image = Image.Load<Rgba32>(await response.Content.ReadAsByteArrayAsync());
        Assert.AreEqual(2, image.Width);
        Assert.AreEqual(1, image.Height);
        Assert.AreEqual(new Rgba32(0, 0, 0, 0), image[0, 0]);
        Assert.AreEqual(new Rgba32(0, 0, 0, 0), image[1, 0]);
    }

    [TestMethod]
    [DataRow("weak")]
    [DataRow("strong")]
    [DataRow("list")]
    [DataRow("wildcard")]
    public async Task ImageRequest_MatchingValidatorReturnsEmptyNotModifiedWithMaterial(string condition)
    {
        var store = CreateStore();
        AddImage(store, 9, 1, new Rgba32(100, 110, 120));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTextureEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var initial = await client.GetAsync("/api/v1/cache/textures/0/image");
        Assert.AreEqual(HttpStatusCode.OK, initial.StatusCode);
        Assert.IsNotNull(initial.Headers.CacheControl);
        Assert.IsTrue(initial.Headers.CacheControl.Private);
        Assert.IsTrue(initial.Headers.CacheControl.NoCache);
        Assert.IsNotNull(initial.Headers.ETag);
        Assert.IsTrue(initial.Headers.ETag.IsWeak);
        var etag = initial.Headers.ETag.ToString();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/cache/textures/0/image");
        request.Headers.TryAddWithoutValidation("If-None-Match", condition switch
        {
            "weak" => etag,
            "strong" => initial.Headers.ETag.Tag,
            "list" => $"\"unrelated\", {etag}",
            "wildcard" => "*",
            _ => throw new ArgumentOutOfRangeException(nameof(condition))
        });

        using var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.NotModified, response.StatusCode);
        Assert.AreEqual(0, (await response.Content.ReadAsByteArrayAsync()).Length);
        Assert.AreEqual(etag, response.Headers.ETag?.ToString());
        Assert.AreEqual(initial.Headers.GetValues("X-Texture-Material").Single(), response.Headers.GetValues("X-Texture-Material").Single());
        Assert.IsNotNull(response.Headers.CacheControl);
        Assert.IsTrue(response.Headers.CacheControl.Private);
        Assert.IsTrue(response.Headers.CacheControl.NoCache);
    }

    [TestMethod]
    public async Task ImageRequest_ReusesSuccessfulOutputForUnconditionalGet()
    {
        var store = CreateStore();
        AddImage(store, 9, 1, new Rgba32(100, 110, 120));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTextureEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var initial = await client.GetAsync("/api/v1/cache/textures/0/image");
        Assert.AreEqual(HttpStatusCode.OK, initial.StatusCode);
        Assert.IsNull(initial.Headers.Age);
        var png = await initial.Content.ReadAsByteArrayAsync();

        using var repeated = await client.GetAsync("/api/v1/cache/textures/0/image");
        Assert.AreEqual(HttpStatusCode.OK, repeated.StatusCode);
        Assert.IsNotNull(repeated.Headers.Age);
        Assert.AreEqual(initial.Headers.ETag, repeated.Headers.ETag);
        CollectionAssert.AreEqual(png, await repeated.Content.ReadAsByteArrayAsync());
    }

    [TestMethod]
    public async Task ImageRequest_RawTextureWriteEvictsCachedOutput()
    {
        var store = CreateStore();
        AddImage(store, 9, 1, new Rgba32(100, 110, 120));
        await using var app = await CacheServiceTestHost.StartAsync(store, group =>
        {
            group.MapTextureEndpoints();
            group.MapStoreMutationEndpoints();
        });
        using var client = CacheServiceTestHost.CreateClient(app);
        using var initial = await client.GetAsync("/api/v1/cache/textures/0/image");
        Assert.AreEqual(HttpStatusCode.OK, initial.StatusCode);

        using var image = new Image<Rgba32>(1, 1, new Rgba32(1, 2, 3));
        using var bytes = new MemoryStream(); image.SaveAsPng(bytes);
        using var container = new Container(new MemoryStream(bytes.ToArray()));
        using var write = await client.PostAsJsonAsync("/api/v1/cache/store/indexes/9/files/0/actions/write-raw",
            new WriteRawFileDto(Convert.ToBase64String(container.Encode())));
        Assert.AreEqual(HttpStatusCode.OK, write.StatusCode, await write.Content.ReadAsStringAsync());

        using var repeated = await client.GetAsync("/api/v1/cache/textures/0/image");
        Assert.IsNull(repeated.Headers.Age);
        Assert.AreNotEqual(initial.Headers.ETag, repeated.Headers.ETag);
    }

    [TestMethod]
    [DataRow(9)]
    [DataRow(26)]
    public async Task ImageRequest_ContainerWriteToTextureSourceEvictsCachedOutput(int indexId)
    {
        var store = CreateStore();
        AddImage(store, 9, 1, new Rgba32(100, 110, 120));
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var archive = new ReferenceTableEntry(0);
        archive.InitializeEntries(1); archive.AddEntry(0, new ReferenceTableChildEntry(0)); table.AddEntry(0, archive);
        using var encoded = new ReferenceTableCodec().Encode(table);
        using var reference = new Container(new MemoryStream(encoded.ToArray()));
        store.Set(255, 9, reference.Encode());
        await using var app = await CacheServiceTestHost.StartAsync(store, group =>
        {
            group.MapTextureEndpoints();
            group.MapContainerEndpoints();
        });
        using var client = CacheServiceTestHost.CreateClient(app);
        using var initial = await client.GetAsync("/api/v1/cache/textures/0/image");
        Assert.AreEqual(HttpStatusCode.OK, initial.StatusCode);

        // Rewrite the current texture image or material table with new bytes, keeping both decodable.
        byte[] payload;
        if (indexId == 9)
        {
            using var image = new Image<Rgba32>(1, 1, new Rgba32(1, 2, 3));
            using var bytes = new MemoryStream(); image.SaveAsPng(bytes);
            payload = bytes.ToArray();
        }
        else
        {
            using var material = store.Read(26, 0);
            using var decoded = new ContainerDecoder().Decode(material);
            payload = decoded.Data.ToArray();
            payload[3 + 5] ^= 1;
        }
        using var write = await client.PostAsJsonAsync($"/api/v1/cache/indexes/{indexId}/files/0/actions/write-container",
            new WriteContainerDto(Convert.ToBase64String(payload)));
        Assert.AreEqual(HttpStatusCode.OK, write.StatusCode, await write.Content.ReadAsStringAsync());

        using var repeated = await client.GetAsync("/api/v1/cache/textures/0/image");
        Assert.AreEqual(HttpStatusCode.OK, repeated.StatusCode, await repeated.Content.ReadAsStringAsync());
        Assert.IsNull(repeated.Headers.Age);
        Assert.AreNotEqual(initial.Headers.ETag, repeated.Headers.ETag);
    }

    [TestMethod]
    public async Task ImageRequest_NonmatchingValidatorReturnsPng()
    {
        var store = CreateStore();
        AddImage(store, 9, 1, new Rgba32(100, 110, 120));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTextureEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/cache/textures/0/image");
        request.Headers.TryAddWithoutValidation("If-None-Match", "W/\"unrelated\"");

        using var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(response.Headers.ETag);
        using var image = Image.Load<Rgba32>(await response.Content.ReadAsByteArrayAsync());
        Assert.AreEqual(new Rgba32(100, 110, 120, 255), image[0, 0]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ImageRequest_CacheMutationInvalidatesValidator(bool changeMaterial)
    {
        var store = CreateStore();
        AddImage(store, 9, 1, new Rgba32(100, 110, 120));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTextureEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var initial = await client.GetAsync("/api/v1/cache/textures/0/image");
        Assert.AreEqual(HttpStatusCode.OK, initial.StatusCode);
        Assert.IsNotNull(initial.Headers.ETag);
        var oldPixels = await initial.Content.ReadAsByteArrayAsync();
        var oldMaterial = initial.Headers.GetValues("X-Texture-Material").Single();
        var cache = app.Services.GetRequiredService<ICacheAPI>();
        if (changeMaterial)
        {
            using var source = cache.Read(26, 0, 0);
            var data = source.ToArray();
            data[3 + 3] = 90; // Brightness changes material semantics without changing image pixels.
            cache.ReplaceArchiveMember(26, 0, 0, data);
        }
        else
        {
            using var image = new Image<Rgba32>(1, 1, new Rgba32(200, 210, 220));
            using var bytes = new MemoryStream();
            image.SaveAsPng(bytes);
            using var container = new Container(new MemoryStream(bytes.ToArray()));
            using var encoded = new MemoryStream(container.Encode());
            cache.WriteRawFile(9, 0, encoded);
        }
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/cache/textures/0/image");
        request.Headers.IfNoneMatch.Add(initial.Headers.ETag);

        using var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(response.Headers.ETag);
        Assert.AreNotEqual(initial.Headers.ETag, response.Headers.ETag);
        var pixels = await response.Content.ReadAsByteArrayAsync();
        using var decoded = Image.Load<Rgba32>(pixels);
        if (changeMaterial)
        {
            Assert.AreNotEqual(oldMaterial, response.Headers.GetValues("X-Texture-Material").Single());
            var material = JsonSerializer.Deserialize<GetTextureMaterialDto>(response.Headers.GetValues("X-Texture-Material").Single(), JsonSerializerOptions.Web);
            Assert.IsNotNull(material);
            Assert.AreEqual(90, material.Brightness);
            CollectionAssert.AreEqual(oldPixels, pixels);
        }
        else
        {
            Assert.AreEqual(oldMaterial, response.Headers.GetValues("X-Texture-Material").Single());
            Assert.AreEqual(new Rgba32(200, 210, 220, 255), decoded[0, 0]);
            CollectionAssert.AreNotEqual(oldPixels, pixels);
        }
    }

    [TestMethod]
    public async Task HdrRequest_ReturnsFloatIntensitiesAndKeepsPngCompatible()
    {
        var store = CreateStore();
        AddImage(store, 9, 1, new Rgba32(255, 128, 0, 255));
        AddImage(store, 37, 1, new Rgba32(0, 8, 0, 255));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTextureEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var result = await client.GetFromJsonAsync<GetTextureHdrDto>("/api/v1/cache/textures/0/hdr");
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Width); Assert.AreEqual(1, result.Height);
        CollectionAssert.AreEqual(new float[] {16.5f, 128/255f*16.5f, 0, 1}, result.Rgba);
        using var png = await client.GetAsync("/api/v1/cache/textures/0/image");
        Assert.AreEqual(HttpStatusCode.OK, png.StatusCode);
        Assert.AreEqual("image/png", png.Content.Headers.ContentType!.MediaType);
        Assert.AreEqual("1", png.Headers.GetValues("X-Texture-Alpha-Mode").Single());
        Assert.AreEqual("170", png.Headers.GetValues("X-Texture-Color-Blend").Single());
        Assert.AreEqual("25", png.Headers.GetValues("X-Texture-Brightness").Single());
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

    private static MemoryFileStore CreateStore(byte effectId = 2, byte alphaMode = 1)
    {
        var store = new MemoryFileStore();
        var data = new byte[26];
        data[1] = 1; data[2] = 1;
        data[3 + 3] = 25; data[3 + 4] = 170;
        data[3 + 5] = effectId; data[3 + 6] = 12;
        data[3 + 9] = 255; data[3 + 10] = 4;
        data[3 + 14] = 1; data[3 + 16] = 1;
        data[^1] = alphaMode;
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
