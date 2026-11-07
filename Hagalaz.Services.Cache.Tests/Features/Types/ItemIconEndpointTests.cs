using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features.Mutations;
using Hagalaz.Services.Cache.Features.Types;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public sealed class ItemIconEndpointTests
{
    // Legacy single-triangle model: (0,0,0), (10,0,0), (0,20,0) before the legacy ×4 unit scale. Its front faces -Z,
    // so the test items yaw half a turn (rotation2 1024) to face the icon camera.
    private static readonly byte[] TriangleModel = [0, 1, 3, 1, 2, 64, 65, 65, 0, 42, 0, 0, 0, 1, 0, 2, 74, 54, 84,
        0, 3, 0, 1, 1, 1, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3];

    private const string Url = "/api/v1/cache/types/items/0/image";

    [TestMethod]
    public async Task Image_RendersClientDefaultIconWithOutlineShadowAndRevalidation()
    {
        await using var app = await StartAsync(Seed((0, Item(zoom: 800))));
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.GetAsync(Url);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.AreEqual("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.IsTrue(response.Headers.CacheControl!.Private);
        Assert.IsTrue(response.Headers.CacheControl.NoCache);
        Assert.IsNotNull(response.Headers.ETag);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var pixels = Pixels(bytes, out var width, out var height);
        Assert.AreEqual(36, width);
        Assert.AreEqual(32, height);
        Assert.IsTrue(pixels.Any(pixel => pixel.A == 255 && pixel.Rgb != 0x000002 && pixel.Rgb != 0x302020), "The model is drawn.");
        Assert.IsTrue(pixels.Any(pixel => pixel.Rgb == 0x000002 && pixel.A == 255), "The client outline is drawn.");
        Assert.IsTrue(pixels.Any(pixel => pixel.Rgb == 0x302020 && pixel.A == 255), "The inventory shadow is drawn.");
        Assert.IsTrue(pixels.All(pixel => pixel.A is 0 or 255));

        using var repeated = await client.GetAsync(Url);
        CollectionAssert.AreEqual(bytes, await repeated.Content.ReadAsByteArrayAsync());
        Assert.AreEqual(response.Headers.ETag, repeated.Headers.ETag);

        using var conditional = new HttpRequestMessage(HttpMethod.Get, Url);
        conditional.Headers.IfNoneMatch.Add(response.Headers.ETag!);
        using var notModified = await client.SendAsync(conditional);
        Assert.AreEqual(HttpStatusCode.NotModified, notModified.StatusCode);
        Assert.AreEqual(response.Headers.ETag, notModified.Headers.ETag);
        Assert.IsTrue(notModified.Headers.CacheControl!.NoCache);
        Assert.AreEqual(0, (await notModified.Content.ReadAsByteArrayAsync()).Length);
    }

    [TestMethod]
    public async Task Image_AppliesSizeOutlineAndShadowOptionsWithDistinctValidators()
    {
        await using var app = await StartAsync(Seed((0, Item(zoom: 800))));
        using var client = CacheServiceTestHost.CreateClient(app);

        var (large, largeTag) = await GetAsync(client, "?width=72&height=64");
        Pixels(large, out var width, out var height);
        Assert.AreEqual((72, 64), (width, height));

        var (plain, plainTag) = await GetAsync(client, "?outline=0&shadow=none");
        var plainPixels = Pixels(plain, out _, out _);
        Assert.IsFalse(plainPixels.Any(pixel => pixel.Rgb is 0x000002 or 0x302020));
        Assert.IsTrue(plainPixels.Any(pixel => pixel.A == 255));

        var (light, lightTag) = await GetAsync(client, "?outline=2");
        var lightPixels = Pixels(light, out _, out _);
        Assert.IsTrue(lightPixels.Any(pixel => pixel.Rgb == 0xFFFFFF && pixel.A == 255), "Level 2 adds the light outer outline.");
        Assert.IsTrue(lightPixels.Any(pixel => pixel.Rgb == 0x000002));

        var (red, redTag) = await GetAsync(client, "?shadow=ff0000");
        Assert.IsTrue(Pixels(red, out _, out _).Any(pixel => pixel.Rgb == 0xFF0000 && pixel.A == 255));

        var (_, defaultTag) = await GetAsync(client, "");
        CollectionAssert.AllItemsAreUnique(new[] { largeTag, plainTag, lightTag, redTag, defaultTag });
    }

    [TestMethod]
    [DataRow("?width=7")]
    [DataRow("?height=513")]
    [DataRow("?outline=3")]
    [DataRow("?outline=-1")]
    [DataRow("?shadow=red")]
    [DataRow("?shadow=12345")]
    [DataRow("?shadow=gg0000")]
    public async Task Image_RejectsInvalidOptions(string query)
    {
        await using var app = await StartAsync(Seed((0, Item(zoom: 800))));
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync(Url + query);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreNotEqual("image/png", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task Image_ReturnsNotFoundForMissingModelsAndUnprocessableForUnsupportedModels()
    {
        var store = Seed((1, Item(zoom: 800, modelId: 5)), (2, Item(zoom: 800, modelId: 6)));
        var unsupported = new byte[23];
        unsupported[5] = 16;
        unsupported[^1] = 255; unsupported[^2] = 255;
        store.Set(7, 6, ContainerBytes(unsupported));
        await using var app = await StartAsync(store);
        using var client = CacheServiceTestHost.CreateClient(app);

        using var missingModel = await client.GetAsync("/api/v1/cache/types/items/1/image");
        Assert.AreEqual(HttpStatusCode.NotFound, missingModel.StatusCode);
        Assert.AreEqual(0, (await missingModel.Content.ReadAsByteArrayAsync()).Length);
        using var badModel = await client.GetAsync("/api/v1/cache/types/items/2/image");
        Assert.AreEqual(HttpStatusCode.UnprocessableEntity, badModel.StatusCode);
        StringAssert.Contains(await badModel.Content.ReadAsStringAsync(), "Unsupported model format");
    }

    [TestMethod]
    public async Task Image_CompositesNotedAndLentItemsOnTheirTemplates()
    {
        // 2 is the original item, 3 the template, 4 its note and 5 its lend.
        var store = Seed((2, Item(zoom: 800, recolor: 21000)), (3, Item(zoom: 900, rotation1: 64)),
            (4, Item(zoom: 800, note: (2, 3))), (5, Item(zoom: 800, lend: (2, 3))));
        await using var app = await StartAsync(store);
        using var client = CacheServiceTestHost.CreateClient(app);

        var template = await client.GetByteArrayAsync("/api/v1/cache/types/items/3/image?quantityMode=0");
        var noted = await client.GetByteArrayAsync("/api/v1/cache/types/items/4/image?quantityMode=0");
        var lent = await client.GetByteArrayAsync("/api/v1/cache/types/items/5/image?quantityMode=0");
        CollectionAssert.AreNotEqual(template, noted, "The note draws the original item over its template.");
        CollectionAssert.AreNotEqual(template, lent, "The lend draws its template over the original item.");
        CollectionAssert.AreNotEqual(noted, lent);
        // Pinned renders guard the client composition order.
        Assert.AreEqual(NotedHash, Convert.ToHexStringLower(SHA256.HashData(Pixels(noted, out _, out _).Select(pixel => pixel.Argb).SelectMany(BitConverter.GetBytes).ToArray())));
        Assert.AreEqual(LentHash, Convert.ToHexStringLower(SHA256.HashData(Pixels(lent, out _, out _).Select(pixel => pixel.Argb).SelectMany(BitConverter.GetBytes).ToArray())));
    }

    [TestMethod]
    public async Task Image_RevalidatesWithoutRenderingAndChangesValidatorAfterRawWrites()
    {
        var store = Seed((0, Item(zoom: 800)));
        await using var app = await CacheServiceTestHost.StartAsync(store, group =>
        {
            group.MapTypeEndpoints();
            group.MapStoreMutationEndpoints();
        });
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync(Url);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var modelReads = store.GetReadCount(7, 0);

        using var conditional = new HttpRequestMessage(HttpMethod.Get, Url);
        conditional.Headers.IfNoneMatch.Add(response.Headers.ETag!);
        using var notModified = await client.SendAsync(conditional);
        Assert.AreEqual(HttpStatusCode.NotModified, notModified.StatusCode);
        Assert.AreEqual(modelReads, store.GetReadCount(7, 0), "A matching validator must not render the model.");

        // Raw writes keep reference-table versions, so the validator must still change.
        using var raw = await client.PostAsJsonAsync("/api/v1/cache/store/indexes/7/files/0/actions/copy-raw", new { sourceIndexId = 7, sourceFileId = 0 });
        Assert.AreEqual(HttpStatusCode.OK, raw.StatusCode, await raw.Content.ReadAsStringAsync());
        using var afterRaw = new HttpRequestMessage(HttpMethod.Get, Url);
        afterRaw.Headers.IfNoneMatch.Add(response.Headers.ETag!);
        using var rerendered = await client.SendAsync(afterRaw);
        Assert.AreEqual(HttpStatusCode.OK, rerendered.StatusCode);
        Assert.AreNotEqual(response.Headers.ETag, rerendered.Headers.ETag);
    }

    [TestMethod]
    public async Task Image_FollowsDefinitionEditsAndDuplicates()
    {
        await using var app = await StartAsync(Seed((0, Item(zoom: 800))));
        using var client = CacheServiceTestHost.CreateClient(app);
        var (before, beforeTag) = await GetAsync(client, "");

        var definition = await client.GetFromJsonAsync<UpdateItemDefinitionDto>("/api/v1/cache/types/items/0/definition");
        Assert.IsNotNull(definition);
        definition.ModelZoom = 1200;
        using var update = await client.PutAsJsonAsync("/api/v1/cache/types/items/0/definition", definition);
        Assert.AreEqual(HttpStatusCode.OK, update.StatusCode, await update.Content.ReadAsStringAsync());
        var (after, afterTag) = await GetAsync(client, "");
        CollectionAssert.AreNotEqual(before, after);
        Assert.AreNotEqual(beforeTag, afterTag);

        using var duplicate = await client.PostAsJsonAsync("/api/v1/cache/types/items/0/duplicate", new { });
        Assert.AreEqual(HttpStatusCode.Created, duplicate.StatusCode, await duplicate.Content.ReadAsStringAsync());
        var copy = await duplicate.Content.ReadFromJsonAsync<DuplicateItemDefinitionResultDto>();
        Assert.IsNotNull(copy);
        var duplicated = await client.GetByteArrayAsync($"/api/v1/cache/types/items/{copy.Id}/image");
        CollectionAssert.AreEqual(after, duplicated);
    }

    [TestMethod]
    [DataRow("?quantity=-2")]
    [DataRow("?quantityMode=3")]
    [DataRow("?quantityMode=-1")]
    public async Task Image_RejectsInvalidQuantityOptions(string query)
    {
        await using var app = await StartAsync(Seed((0, Item(zoom: 800))));
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync(Url + query);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Image_UsesTheStackVariantModelForTheQuantity()
    {
        // Item 0 switches to item 1 from five units; item 1 is the same model at a different zoom.
        await using var app = await StartAsync(Seed((0, Item(zoom: 800, stack: (1, 5))), (1, Item(zoom: 1400))));
        using var client = CacheServiceTestHost.CreateClient(app);
        var single = await client.GetByteArrayAsync("/api/v1/cache/types/items/0/image?quantity=4&quantityMode=0");
        var stacked = await client.GetByteArrayAsync("/api/v1/cache/types/items/0/image?quantity=5&quantityMode=0");
        var variant = await client.GetByteArrayAsync("/api/v1/cache/types/items/1/image?quantity=5&quantityMode=0");
        CollectionAssert.AreNotEqual(single, stacked);
        CollectionAssert.AreEqual(variant, stacked);
    }

    [TestMethod]
    public async Task Image_DrawsTheClientQuantityLabelForTheMode()
    {
        var store = Seed((0, Item(zoom: 800, stackable: true)), (1, Item(zoom: 800)));
        SeedFont(store);
        await using var app = await StartAsync(store);
        using var client = CacheServiceTestHost.CreateClient(app);

        // By default only stackable items with a positive quantity are labelled, including quantity one.
        var stackable = Pixels(await client.GetByteArrayAsync(Url), out _, out _);
        Assert.IsTrue(stackable.Any(pixel => pixel.Rgb == 0xFFFF00 && pixel.A == 255), "Yellow quantity text is drawn.");
        Assert.IsTrue(stackable.Any(pixel => pixel.Rgb == 0x000001 && pixel.A == 255), "The text shadow is drawn.");
        var ordinary = Pixels(await client.GetByteArrayAsync("/api/v1/cache/types/items/1/image"), out _, out _);
        Assert.IsFalse(ordinary.Any(pixel => pixel.Rgb == 0xFFFF00));
        Assert.IsFalse(Pixels(await client.GetByteArrayAsync("/api/v1/cache/types/items/1/image?quantity=2"), out _, out _).Any(pixel => pixel.Rgb == 0xFFFF00), "Ordinary items are never labelled by default.");
        Assert.IsFalse(Pixels(await client.GetByteArrayAsync(Url + "?quantity=0"), out _, out _).Any(pixel => pixel.Rgb == 0xFFFF00), "A zero quantity is not labelled.");
        // The explicit client modes remain available: mode 2 also labels ordinary items with a quantity other than one.
        Assert.IsTrue(Pixels(await client.GetByteArrayAsync("/api/v1/cache/types/items/1/image?quantity=2&quantityMode=2"), out _, out _).Any(pixel => pixel.Rgb == 0xFFFF00));
        Assert.IsTrue(Pixels(await client.GetByteArrayAsync("/api/v1/cache/types/items/1/image?quantityMode=1"), out _, out _).Any(pixel => pixel.Rgb == 0xFFFF00));
        Assert.IsFalse(Pixels(await client.GetByteArrayAsync(Url + "?quantityMode=0"), out _, out _).Any(pixel => pixel.Rgb == 0xFFFF00));

        Assert.IsTrue(Pixels(await client.GetByteArrayAsync(Url + "?quantity=250000"), out _, out _).Any(pixel => pixel.Rgb == 0xFFFFFF), "Thousands are white.");
        Assert.IsTrue(Pixels(await client.GetByteArrayAsync(Url + "?quantity=25000000"), out _, out _).Any(pixel => pixel.Rgb == 0x00FF80), "Millions are green.");
    }

    [TestMethod]
    public async Task Image_ReportsAMissingQuantityFontAsUnprocessable()
    {
        await using var app = await StartAsync(Seed((0, Item(zoom: 800, stackable: true))));
        using var client = CacheServiceTestHost.CreateClient(app);
        using var labelled = await client.GetAsync(Url);
        Assert.AreEqual(HttpStatusCode.UnprocessableEntity, labelled.StatusCode);
        using var unlabelled = await client.GetAsync(Url + "?quantityMode=0");
        Assert.AreEqual(HttpStatusCode.OK, unlabelled.StatusCode);
    }

    private static void SeedFont(MemoryFileStore store)
    {
        var fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures", "fonts");
        foreach (var (index, file) in new[] { (13, "p11_full.metrics"), (8, "p11_full.sprite") })
        {
            var table = new ReferenceTable(7, 1, ReferenceTableFlags.Identifiers, 1);
            var entry = new ReferenceTableEntry(0) { Id = ReferenceTable.CalculateId("p11_full") };
            entry.InitializeEntries(1);
            entry.AddEntry(0, new ReferenceTableChildEntry(0));
            table.AddEntry(0, entry);
            using var encoded = new ReferenceTableCodec().Encode(table);
            store.Set(255, index, ContainerBytes(encoded.ToArray()));
            store.Set(index, 0, ContainerBytes(File.ReadAllBytes(Path.Combine(fixtures, file))));
        }
    }

    private const string NotedHash = "1b047446f4482e79d3383a8450a3cba8e2b47ac28e2e7687ee2725279c1e310b";
    private const string LentHash = "abea9778577f241bdb3204ceaca3e5c53d03cdcea5d6a6b6cd31b78ebe3489cf";

    private readonly record struct Pixel(int Argb)
    {
        public int A => Argb >>> 24;
        public int Rgb => Argb & 0xFFFFFF;
    }

    private static Pixel[] Pixels(byte[] png, out int width, out int height)
    {
        using var image = Image.Load<Rgba32>(png);
        (width, height) = (image.Width, image.Height);
        var pixels = new Pixel[image.Width * image.Height];
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
        {
            var value = image[x, y];
            pixels[y * image.Width + x] = new Pixel(value.A << 24 | value.R << 16 | value.G << 8 | value.B);
        }
        return pixels;
    }

    private static async Task<(byte[] Bytes, string Tag)> GetAsync(HttpClient client, string query)
    {
        using var response = await client.GetAsync(Url + query);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadAsByteArrayAsync(), response.Headers.ETag!.Tag);
    }

    private static byte[] Item(int zoom, int modelId = 0, int rotation1 = 0, int rotation2 = 1024, int? recolor = null, (int Id, int Template)? note = null, (int Id, int Template)? lend = null,
        bool stackable = false, (int Id, int Amount)? stack = null)
    {
        var bytes = new List<byte>();
        bytes.AddRange([1, (byte)(modelId >> 8), (byte)modelId]);
        bytes.AddRange([4, (byte)(zoom >> 8), (byte)zoom]);
        if (rotation1 != 0) bytes.AddRange([5, (byte)(rotation1 >> 8), (byte)rotation1]);
        if (rotation2 != 0) bytes.AddRange([6, (byte)(rotation2 >> 8), (byte)rotation2]);
        if (recolor is { } color) bytes.AddRange([40, 1, 0, 42, (byte)(color >> 8), (byte)color]);
        if (note is { } noted) bytes.AddRange([97, (byte)(noted.Id >> 8), (byte)noted.Id, 98, (byte)(noted.Template >> 8), (byte)noted.Template]);
        if (lend is { } lent) bytes.AddRange([121, (byte)(lent.Id >> 8), (byte)lent.Id, 122, (byte)(lent.Template >> 8), (byte)lent.Template]);
        if (stackable) bytes.Add(11);
        if (stack is { } variant) bytes.AddRange([100, (byte)(variant.Id >> 8), (byte)variant.Id, (byte)(variant.Amount >> 8), (byte)variant.Amount]);
        bytes.Add(0);
        return bytes.ToArray();
    }

    private static MemoryFileStore Seed(params (int Id, byte[] Data)[] items)
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var archive = new ReferenceTableEntry(0);
        var members = items.OrderBy(item => item.Id).ToArray();
        archive.InitializeEntries(members.Length);
        foreach (var member in members) archive.AddEntry(member.Id, new ReferenceTableChildEntry(member.Id));
        table.AddEntry(0, archive);
        using var bytes = new MemoryStream();
        foreach (var member in members) bytes.Write(member.Data);
        if (members.Length > 1)
        {
            var previous = 0;
            foreach (var member in members)
            {
                var delta = new byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(delta, member.Data.Length - previous);
                bytes.Write(delta);
                previous = member.Data.Length;
            }
            bytes.WriteByte(1);
        }
        store.Set(19, 0, ContainerBytes(bytes.ToArray()));
        using var encoded = new ReferenceTableCodec().Encode(table);
        store.Set(255, 19, ContainerBytes(encoded.ToArray()));
        store.Set(7, 0, ContainerBytes(TriangleModel));
        return store;
    }

    private static Task<Microsoft.AspNetCore.Builder.WebApplication> StartAsync(MemoryFileStore store) =>
        CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());

    private static byte[] ContainerBytes(byte[] data)
    {
        using var container = new Container(new MemoryStream(data));
        return container.Encode();
    }
}
