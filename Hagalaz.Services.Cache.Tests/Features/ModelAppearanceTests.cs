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
    [TestMethod]
    [DataRow("items", 19)]
    [DataRow("npcs", 18)]
    [DataRow("objects", 16)]
    public async Task DefinitionModel_AppliesScaleAndOrderedUnsignedAppearancePairs(string kind, int index)
    {
        var store = new MemoryFileStore();
        SeedDefinition(store, index, Definition(kind));
        byte[] model = [0, 1, 3, 1, 2, 64, 65, 65, 0, 42, 0, 0, 0, 1, 0, 2, 74, 54, 84,
            0, 3, 0, 1, 1, 1, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3];
        store.Set(7, 0, ContainerBytes(model));
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
