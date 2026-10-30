using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public sealed class ModelAppearanceTests
{
    private static readonly byte[] LegacyModel = [0, 1, 3, 1, 2, 64, 65, 65, 0, 42, 0, 0, 0, 1, 0, 2, 74, 54, 84,
            0, 3, 0, 1, 1, 1, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3];
    private static readonly byte[] ModernSkinnedModel = [0, 1, 3, 1, 1, 2, 3, 64, 65, 65, 0x12, 0x34, 74, 54, 84, 13,
        0, 3, 0, 1, 0, 8, 0, 0, 0, 0, 1, 0, 2, 0, 1, 0, 0, 0, 3, 0, 0, 255, 255];

    [TestMethod]
    [DataRow("items", 19)]
    [DataRow("npcs", 18)]
    [DataRow("objects", 16)]
    public async Task DefinitionModel_AppliesScaleAndOrderedUnsignedAppearancePairs(string kind, int index)
    {
        var store = new MemoryFileStore();
        SeedDefinition(store, index, Definition(kind));
        store.Set(7, 0, ContainerBytes(LegacyModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        var result = await client.GetFromJsonAsync<ModelEndpoints.ModelResponse>($"/api/v1/cache/types/{kind}/0/model");

        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 60000 }, result.FaceTextureIds);
        CollectionAssert.AreEqual(new[] { 50000 }, result.FaceColors);
        CollectionAssert.AreEqual(kind == "npcs" ? new[] { 0, 0, 0, 40, 0, 0, 0, 80, 0 } : new[] { 0, 0, 0, 80, 0, 0, 0, 40, 0 }, result.Positions);
        if (kind == "npcs") CollectionAssert.AreEqual(new float[] { 2, 0.5f, 2 }, result.Scale);
        else Assert.IsNull(result.Scale);
        Assert.AreEqual(1f, result.CoordinateScale);
        Assert.IsNull(result.VertexSkins, "A definition with no skinned vertices must not offer manual animation posing.");
        CollectionAssert.AreEqual(new float[] { 0, 0, 1, 0, 0, 1 }, result.FaceTextureUvs);

        var raw = await client.GetFromJsonAsync<ModelEndpoints.ModelResponse>("/api/v1/cache/models/0");
        Assert.IsNotNull(raw);
        Assert.AreEqual(0.25f, raw.CoordinateScale);
        CollectionAssert.AreEqual(new[] { 42 }, raw.FaceTextureIds);
        CollectionAssert.AreEqual(new[] { 127 }, raw.FaceColors);
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 10, 0, 0, 0, 20, 0 }, raw.Positions);
    }

    [TestMethod]
    public async Task NpcModel_MixedVersionsNormalizeCoordinatesAndPreserveSkinLabels()
    {
        var store = new MemoryFileStore();
        SeedDefinition(store, 18, new byte[] { 1, 2, 0, 0, 0, 1 }.Concat(Definition("npcs").Skip(4)).ToArray());
        store.Set(7, 0, ContainerBytes(LegacyModel));
        store.Set(7, 1, ContainerBytes(ModernSkinnedModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        var result = await client.GetFromJsonAsync<ModelEndpoints.ModelResponse>("/api/v1/cache/types/npcs/0/model");

        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 40, 0, 0, 0, 80, 0, 0, 0, 0, 10, 0, 0, 0, 20, 0 }, result.Positions);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, result.Indices);
        CollectionAssert.AreEqual(new[] { -1, -1, -1, 1, 2, 3 }, result.VertexSkins);
        CollectionAssert.AreEqual(new[] { 60000, -1 }, result.FaceTextureIds);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, -1, -1, -1 }, result.FaceTextureVertices);
        Assert.HasCount(12, result.FaceTextureUvs!);
    }

    [TestMethod]
    public async Task ObjectModel_UsesRequestedShapeGroupAndRejectsAbsentShapes()
    {
        var store = new MemoryFileStore();
        SeedDefinition(store, 16, new byte[] { 1, 2, 10, 1, 0, 0, 20, 1, 0, 1 }.Concat(Definition("objects").Skip(6)).ToArray());
        store.Set(7, 0, ContainerBytes(LegacyModel));
        store.Set(7, 1, ContainerBytes(ModernSkinnedModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        var result = await client.GetFromJsonAsync<ModelEndpoints.ModelResponse>("/api/v1/cache/types/objects/0/model?shape=20");

        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 20, 0, 0, 0, 10, 0 }, result.Positions);
        CollectionAssert.AreEqual(new[] { 0x1234 }, result.FaceColors);
        using var absent = await client.GetAsync("/api/v1/cache/types/objects/0/model?shape=22");
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, absent.StatusCode);
    }

    [TestMethod]
    [DataRow("male-equipped", 3, 4, -8, 12, 40)]
    [DataRow("female-equipped", 3, -4, 8, -12, 10)]
    [DataRow("male-head", 2, 0, 0, 0, 40)]
    [DataRow("female-head", 2, 0, 0, 0, 10)]
    public async Task ItemAppearance_AssemblesSelectedPartsWithoutInventoryScale(string appearance, int parts, int x, int y, int z, int firstWidth)
    {
        var store = new MemoryFileStore();
        byte[] equipment = [23,0,0, 24,0,1, 78,0,2, 25,0,1, 26,0,0, 79,0,2,
            90,0,0, 92,0,1, 91,0,1, 93,0,0, 125,1,254,3, 126,255,2,253];
        SeedDefinition(store, 19, equipment.Concat(Definition("items")).ToArray());
        store.Set(7, 0, ContainerBytes(LegacyModel));
        store.Set(7, 1, ContainerBytes(ModernSkinnedModel));
        store.Set(7, 2, ContainerBytes(LegacyModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var metadata = await client.GetFromJsonAsync<ModelEndpoints.ItemAppearance[]>("/api/v1/cache/types/items/0/model-appearances");
        Assert.IsNotNull(metadata); Assert.HasCount(5, metadata);
        var selected = metadata.Single(value => value.Key == appearance);
        Assert.IsTrue(selected.Available); Assert.HasCount(parts, selected.ModelIds);
        CollectionAssert.AreEqual(appearance switch {
            "male-equipped" => new[] {0,1,2}, "female-equipped" => new[] {1,0,2},
            "male-head" => new[] {0,1}, _ => new[] {1,0}}, selected.ModelIds);
        var result = await client.GetFromJsonAsync<ModelEndpoints.ModelResponse>($"/api/v1/cache/types/items/0/model?appearance={appearance}");
        Assert.IsNotNull(result); Assert.HasCount(parts * 9, result.Positions);
        CollectionAssert.AreEqual(new[] {x,y,z, x+firstWidth,y,z}, result.Positions.Take(6).ToArray());
        for (var part = 0; part < parts; part++)
            CollectionAssert.AreEqual(new[] {x,y,z}, result.Positions.Skip(part * 9).Take(3).ToArray(), "Wear translation applies once to every assembled part.");
        CollectionAssert.AreEqual(Enumerable.Range(0, parts*3).ToArray(), result.Indices);
        Assert.AreEqual(parts, result.FaceColors.Length);
        Assert.IsTrue(result.FaceColors.Contains(50000));
        Assert.IsTrue(result.FaceTextureIds!.Contains(60000));
        Assert.AreEqual(1f, result.CoordinateScale);
        Assert.AreEqual(x, selected.OffsetX); Assert.AreEqual(y, selected.OffsetY); Assert.AreEqual(z, selected.OffsetZ);
    }

    [TestMethod]
    public async Task ItemAppearances_RejectAbsentPrimaryAndIgnoreOrphanTertiary()
    {
        var store = new MemoryFileStore();
        SeedDefinition(store, 19, new byte[] {23,0,0, 78,0,2, 26,0,1}.Concat(Definition("items")).ToArray());
        store.Set(7,0,ContainerBytes(LegacyModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var metadata = await client.GetFromJsonAsync<ModelEndpoints.ItemAppearance[]>("/api/v1/cache/types/items/0/model-appearances");
        Assert.IsNotNull(metadata);
        CollectionAssert.AreEqual(new[] {0}, metadata.Single(value => value.Key == "male-equipped").ModelIds);
        Assert.IsFalse(metadata.Single(value => value.Key == "female-equipped").Available);
        var male = await client.GetFromJsonAsync<ModelEndpoints.ModelResponse>("/api/v1/cache/types/items/0/model?appearance=male-equipped");
        Assert.IsNotNull(male); Assert.HasCount(9, male.Positions);
        using var absent = await client.GetAsync("/api/v1/cache/types/items/0/model?appearance=female-equipped");
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, absent.StatusCode);
        using var invalid = await client.GetAsync("/api/v1/cache/types/items/0/model?appearance=other");
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    private static byte[] Definition(string kind)
    {
        using var stream = new MemoryStream();
        if (kind == "items") stream.Write(new byte[] { 1, 0, 0 });
        else if (kind == "npcs") stream.Write(new byte[] { 1, 1, 0, 0 });
        else stream.Write(new byte[] { 1, 1, 10, 1, 0, 0 });
        stream.WriteByte(40); stream.WriteByte(2);
        Short(127); Short(300); Short(300); Short(50000);
        stream.WriteByte(41); stream.WriteByte(2);
        Short(42); Short(50000); Short(50000); Short(60000);
        stream.WriteByte((byte)(kind == "items" ? 110 : kind == "npcs" ? 97 : 65)); Short(256);
        stream.WriteByte((byte)(kind == "items" ? 111 : kind == "npcs" ? 98 : 66)); Short(64);
        stream.WriteByte(0);
        return stream.ToArray();

        void Short(int value) { stream.WriteByte((byte)(value >> 8)); stream.WriteByte((byte)value); }
    }

    private static void SeedDefinition(MemoryFileStore store, int index, byte[] data)
    {
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var archive = new ReferenceTableEntry(0);
        archive.InitializeEntries(1);
        archive.AddEntry(0, new ReferenceTableChildEntry(0));
        table.AddEntry(0, archive);
        using var encoded = new ReferenceTableCodec().Encode(table);
        store.Set(255, index, ContainerBytes(encoded.ToArray()));
        store.Set(index, 0, ContainerBytes(data));
    }

    private static byte[] ContainerBytes(byte[] data)
    {
        using var container = new Container(new MemoryStream(data));
        return container.Encode();
    }
}
