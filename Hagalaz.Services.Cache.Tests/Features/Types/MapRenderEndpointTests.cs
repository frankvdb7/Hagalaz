using System.Net;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Features.Types;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public sealed class MapRenderEndpointTests
{
    private static MemoryFileStore Store(params (string Name, byte[] Payload)[] files)
    {
        var store = new MemoryFileStore();
        var table = new ReferenceTable(7, 1, ReferenceTableFlags.Identifiers, files.Length);
        for (var i = 0; i < files.Length; i++)
        {
            var entry = new ReferenceTableEntry(i) { Id = ReferenceTable.CalculateId(files[i].Name) };
            entry.InitializeEntries(0);
            table.AddEntry(i, entry);
            store.Set(5, i, ContainerBytes(files[i].Payload));
        }
        using var encoded = new ReferenceTableCodec().Encode(table);
        store.Set(255, 5, ContainerBytes(encoded.ToArray()));
        return store;
    }
    private static byte[] ContainerBytes(byte[] bytes) { using var container = new Container(new MemoryStream(bytes)); return container.Encode(); }

    [TestMethod]
    public async Task RenderRegion_UsesAuthoritativeNeighborBordersAndIndependentLandscapeStatus()
    {
        var data = new byte[16384];
        var store = Store(("m1_2", [..data, 0, 1, 0, 0xdd, 0xce, 0xbb]), ("m2_2", data), ("m1_3", data), ("m2_3", data), ("m3_2", data), ("m3_3", data), ("l1_2", new byte[] { 1, 128 }));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/maps/258/render", new { });
        response.EnsureSuccessStatusCode();
        var region = await response.Content.ReadFromJsonAsync<MapRenderRegion>();
        Assert.IsNotNull(region);
        Assert.AreEqual("available", region.TerrainStatus);
        Assert.AreEqual(0xddcebb, region.Atmosphere?.LightRgb);
        Assert.AreEqual("failed", region.LandscapeStatus);
        Assert.HasCount(4, region.Planes);
        Assert.IsEmpty(region.Objects);
        Assert.IsTrue(region.Warnings.Any(warning => warning.Contains("XTEA", StringComparison.Ordinal)));
        using var nextResponse = await client.PostAsJsonAsync("/api/v1/cache/types/maps/514/render", new { xteaKeys = new[] { 0, 0, 0, 0 } });
        nextResponse.EnsureSuccessStatusCode();
        var neighbor = await nextResponse.Content.ReadFromJsonAsync<MapRenderRegion>();
        Assert.IsNotNull(neighbor);
        Assert.IsNull(neighbor.Atmosphere);
        for (var plane = 0; plane < 4; plane++)
        for (var y = 0; y <= 64; y++)
            Assert.AreEqual(neighbor.Planes[plane].CornerHeights[y], region.Planes[plane].CornerHeights[64 * 65 + y]);
    }

    [TestMethod]
    public async Task RenderRegion_ReportsAbsentFilesAndIncompleteBoundaryWithoutGeneratedHeights()
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(("m255_255", [..new byte[16384], 0, 1])), group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var absentResponse = await client.PostAsJsonAsync("/api/v1/cache/types/maps/0/render", new { });
        var absent = await absentResponse.Content.ReadFromJsonAsync<MapRenderRegion>();
        Assert.IsNotNull(absent);
        Assert.AreEqual("absent", absent.TerrainStatus);
        Assert.AreEqual("absent", absent.LandscapeStatus);
        Assert.IsEmpty(absent.Planes);
        using var edgeResponse = await client.PostAsJsonAsync("/api/v1/cache/types/maps/65535/render", new { });
        var edge = await edgeResponse.Content.ReadFromJsonAsync<MapRenderRegion>();
        Assert.IsNotNull(edge);
        Assert.AreEqual("available", edge.TerrainStatus);
        Assert.IsNull(edge.Atmosphere);
        Assert.IsTrue(edge.Warnings.Any(warning => warning.Contains("atmosphere is truncated", StringComparison.Ordinal)));
        Assert.IsNull(edge.Planes[0].CornerHeights[64 * 65]);
        Assert.IsNull(edge.Planes[0].CornerHeights[64]);
        Assert.IsNotNull(edge.Planes[0].CornerHeights[0]);
        Assert.IsTrue(edge.Warnings.Any(warning => warning.Contains("boundary", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task RenderRegion_CorrectKeysRecoverLandscapeWithoutReplacingUsableTerrain()
    {
        var landscape = new byte[] { 1, 0x90, 1, 42, 0, 0 };
        var store = Store(("m0_0", new byte[16384]), ("l0_0", landscape));
        using var container = new Container(CompressionType.Gzip, new MemoryStream(landscape));
        var plain = container.Encode();
        var encrypted = (byte[])plain.Clone();
        Hagalaz.Security.XTEA.Encrypt(plain, encrypted, [uint.MaxValue, 2, 3, 4], 5);
        store.Set(5, 1, encrypted);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var failedResponse = await client.PostAsJsonAsync("/api/v1/cache/types/maps/0/render", new { });
        Assert.AreEqual(HttpStatusCode.OK, failedResponse.StatusCode, await failedResponse.Content.ReadAsStringAsync());
        var failed = await failedResponse.Content.ReadFromJsonAsync<MapRenderRegion>();
        Assert.IsNotNull(failed);
        Assert.AreEqual("available", failed.TerrainStatus);
        Assert.AreEqual("failed", failed.LandscapeStatus);
        using var recoveredResponse = await client.PostAsJsonAsync("/api/v1/cache/types/maps/0/render", new { xteaKeys = new[] { -1, 2, 3, 4 } });
        recoveredResponse.EnsureSuccessStatusCode();
        var recovered = await recoveredResponse.Content.ReadFromJsonAsync<MapRenderRegion>();
        Assert.IsNotNull(recovered);
        Assert.AreEqual("available", recovered.TerrainStatus);
        Assert.AreEqual("available", recovered.LandscapeStatus);
        CollectionAssert.AreEqual(new[] { new MapRenderPlacement(0, 10, 2, 0, 0, 1) }, recovered.Objects.ToArray());
        CollectionAssert.AreEqual(failed.Planes[0].CornerHeights, recovered.Planes[0].CornerHeights);
    }

    [TestMethod]
    public async Task RenderRegion_ResolvesSparseFloorIdsThroughReferenceTable()
    {
        // Tile 0,0 on plane 0 uses underlay 6 (member ID 5), stored at physical position 0 of config group 1.
        var store = Store(("m0_0", [87, ..new byte[16384]]));
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 2);
        var entry = new ReferenceTableEntry(0);
        entry.InitializeEntries(1);
        entry.AddEntry(5, new ReferenceTableChildEntry(0));
        table.AddEntry(1, entry);
        using var encoded = new ReferenceTableCodec().Encode(table);
        store.Set(255, 2, ContainerBytes(encoded.ToArray()));
        store.Set(2, 1, ContainerBytes([1, 0x12, 0x34, 0x56, 0]));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/maps/0/render", new { });
        response.EnsureSuccessStatusCode();
        var region = await response.Content.ReadFromJsonAsync<MapRenderRegion>();
        Assert.IsNotNull(region);
        Assert.AreEqual(6, region.Planes[0].Underlays[0]);
        CollectionAssert.AreEqual(new[] { new FloorRenderDefinition(6, false, 0x123456, -1, null, 512) }, region.Floors.ToArray());
        Assert.IsFalse(region.Warnings.Any(warning => warning.Contains("Underlay", StringComparison.Ordinal)));
    }

    [TestMethod]
    [DataRow(-1, "{}")]
    [DataRow(65536, "{}")]
    [DataRow(0, "{\"xteaKeys\":[1,2,3]}")]
    [DataRow(0, "{\"xteaKeys\":[1.5,2,3,4]}")]
    [DataRow(0, "{\"xteaKeys\":[2147483648,2,3,4]}")]
    public async Task RenderRegion_RejectsInvalidIdentityOrKeysBeforeCacheReads(int id, string body)
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsync($"/api/v1/cache/types/maps/{id}/render", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task RenderRegion_RetainsSourcePlacementsWithoutTerrainAndAcceptsZeroKeys()
    {
        // Object 0 at source plane 1, tile 0,0, shape 10 and rotation 2.
        await using var app = await CacheServiceTestHost.StartAsync(Store(("l0_0", new byte[] { 1, 0x90, 1, 42, 0, 0 })), group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/maps/0/render", new { xteaKeys = new[] { 0, 0, 0, 0 } });
        response.EnsureSuccessStatusCode();
        var region = await response.Content.ReadFromJsonAsync<MapRenderRegion>();
        Assert.IsNotNull(region);
        Assert.AreEqual("absent", region.TerrainStatus);
        Assert.AreEqual("available", region.LandscapeStatus);
        CollectionAssert.AreEqual(new[] { new MapRenderPlacement(0, 10, 2, 0, 0, 1) }, region.Objects.ToArray());
    }
}
