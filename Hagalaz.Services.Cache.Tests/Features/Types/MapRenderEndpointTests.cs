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

    /// <summary>Object IDs 0 to 511 exist.</summary>
    private static void SeedObjectIndex(MemoryFileStore store)
    {
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var archive = new ReferenceTableEntry(1);
        archive.InitializeEntries(1);
        archive.AddEntry(255, new ReferenceTableChildEntry(255));
        table.AddEntry(1, archive);
        using var encoded = new ReferenceTableCodec().Encode(table);
        store.Set(255, 16, ContainerBytes(encoded.ToArray()));
    }

    private static byte[] Landscape(params MapRenderPlacement[] placements)
    {
        using var encoded = MapLandscapeEncoder.Encode(placements);
        return encoded.ToArray();
    }

    private static byte[] Stored(MemoryFileStore store, int file)
    {
        using var data = store.Read(5, file);
        return data.ToArray();
    }

    private const string LandscapeUrl = "/api/v1/cache/types/maps/258/landscape";

    private static (string Name, byte[] Payload)[] EditableRegion(byte[] landscape)
    {
        var terrain = new byte[16384];
        return [("m1_2", terrain), ("l1_2", landscape), ("m2_2", terrain), ("m1_3", terrain), ("m2_3", terrain), ("l2_2", Landscape(new MapRenderPlacement(9, 10, 0, 1, 1, 0)))];
    }

    private const string WorldMapUrl = "/api/v1/cache/types/maps/worldmap/refresh";

    /// <summary>A `main` area with blocks at regions 1,2 and 9,9, shown where they really are.</summary>
    private static void SeedWorldMap(MemoryFileStore store)
    {
        var empties = Enumerable.Repeat((byte)(62 << 2), 4096).ToArray();
        byte[] main = [0, 0, 0, 1, 2, .. empties, 0, 9, 9, .. empties];
        byte[] details = [.. "main"u8, 0, .. "Main"u8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 100, 0, 1, 0, 0, 0, 0, 0, 63, 255, 63, 255, 0, 0, 0, 0, 63, 255, 63, 255];
        var table = new ReferenceTable(7, 1, ReferenceTableFlags.Identifiers, 2);
        var names = new[] { ("main", main), ("details", details) };
        for (var i = 0; i < names.Length; i++)
        {
            var entry = new ReferenceTableEntry(i) { Id = ReferenceTable.CalculateId(names[i].Item1) };
            entry.InitializeEntries(1);
            entry.AddEntry(0, new ReferenceTableChildEntry(0));
            table.AddEntry(i, entry);
            store.Set(23, i, ContainerBytes(names[i].Item2));
        }
        using var encoded = new ReferenceTableCodec().Encode(table);
        store.Set(255, 23, ContainerBytes(encoded.ToArray()));
    }

    private static byte[] StoredWorldMap(MemoryFileStore store)
    {
        using var data = store.Read(23, 0);
        var raw = data.ToArray();
        var length = raw[1] << 24 | raw[2] << 16 | raw[3] << 8 | raw[4];
        return raw[5..(5 + length)];
    }

    private static byte[] TerrainWithUnderlay(byte underlay) => [(byte)(81 + underlay), .. new byte[16384]];

    private sealed record RefreshResult(int[] Refreshed, int[] Unchanged, SkipResult[] Skipped);
    private sealed record SkipResult(int Id, string Reason);

    [TestMethod]
    public async Task RefreshWorldMap_RegeneratesTheRegionAndKeepsOtherBlocks()
    {
        var store = Store(("m1_2", TerrainWithUnderlay(5)), ("l1_2", Landscape()));
        SeedWorldMap(store);
        var before = StoredWorldMap(store);
        var area = WorldMapAreaCodec.Read(before);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(WorldMapUrl, new { regions = new[] { 258 } });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RefreshResult>();
        CollectionAssert.AreEqual(new[] { 258 }, result!.Refreshed);
        var after = StoredWorldMap(store);
        var updated = WorldMapAreaCodec.Read(after);
        Assert.AreEqual(5, WorldMapAreaCodec.DecodeRegion(after, updated, updated.Blocks[0])[0].Layers[0].Underlay);
        CollectionAssert.AreEqual(before[area.Blocks[1].Start..area.Blocks[1].End], after[updated.Blocks[1].Start..updated.Blocks[1].End]);
    }

    [TestMethod]
    public async Task RefreshWorldMap_WritesNothingWhenAlreadyCurrent()
    {
        var store = Store(("m1_2", new byte[16384]));
        SeedWorldMap(store);
        var before = StoredWorldMap(store);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(WorldMapUrl, new { regions = new[] { 258 } });

        var result = await response.Content.ReadFromJsonAsync<RefreshResult>();
        CollectionAssert.AreEqual(new[] { 258 }, result!.Unchanged);
        Assert.IsEmpty(result.Refreshed);
        CollectionAssert.AreEqual(before, StoredWorldMap(store));
    }

    [TestMethod]
    public async Task RefreshWorldMap_SkipsRegionsItCannotTrust()
    {
        var store = Store(("m1_2", [51]), ("m5_5", new byte[16384]));
        SeedWorldMap(store);
        var before = StoredWorldMap(store);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(WorldMapUrl, new { regions = new[] { 258, (5 << 8) | 5 } });

        var result = await response.Content.ReadFromJsonAsync<RefreshResult>();
        CollectionAssert.AreEquivalent(new[] { 258, (5 << 8) | 5 }, result!.Skipped.Select(skip => skip.Id).ToArray());
        CollectionAssert.AreEqual(before, StoredWorldMap(store));
    }

    [TestMethod]
    public async Task RefreshWorldMap_RejectsInvalidRequests()
    {
        var store = Store(("m1_2", new byte[16384]));
        SeedWorldMap(store);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var none = await client.PostAsJsonAsync(WorldMapUrl, new { regions = Array.Empty<int>() });
        using var missing = await client.PostAsJsonAsync(WorldMapUrl, new { });
        using var outOfRange = await client.PostAsJsonAsync(WorldMapUrl, new { regions = new[] { 65536 } });

        Assert.AreEqual(HttpStatusCode.BadRequest, none.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, outOfRange.StatusCode);
    }

    private const string TerrainUrl = "/api/v1/cache/types/maps/258/terrain";

    private static (string Name, byte[] Payload)[] TerrainRegion(byte[] terrain)
    {
        var flat = new byte[16384];
        return [("m1_2", terrain), ("l1_2", Landscape()), ("m2_2", flat), ("m1_3", flat), ("m2_3", flat)];
    }

    [TestMethod]
    public async Task UpdateTerrain_StoresTileChangesAndLeavesOtherArchivesUnchanged()
    {
        byte[] trailing = [2, 9, 8, 7];
        var store = Store(TerrainRegion([.. new byte[16384], .. trailing]));
        var landscape = Stored(store, 1);
        var neighbour = Stored(store, 2);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(TerrainUrl, new { tiles = new object[] { new { plane = 0, x = 2, y = 3, height = 8, underlay = 5, overlay = 7, overlayShape = 1, overlayRotation = 2 } } });

        response.EnsureSuccessStatusCode();
        using var render = await client.PostAsJsonAsync("/api/v1/cache/types/maps/258/render", new { });
        var plane = (await render.Content.ReadFromJsonAsync<MapRenderRegion>())!.Planes[0];
        Assert.AreEqual(-256, plane.CornerHeights[2 * 65 + 3]);
        Assert.AreEqual(5, plane.Underlays[2 * 64 + 3]);
        Assert.AreEqual(7, plane.Overlays[2 * 64 + 3]);
        Assert.AreEqual(1, plane.OverlayShapes[2 * 64 + 3]);
        Assert.AreEqual(2, plane.OverlayRotations[2 * 64 + 3]);
        CollectionAssert.AreEqual(landscape, Stored(store, 1));
        CollectionAssert.AreEqual(neighbour, Stored(store, 2));
    }

    [TestMethod]
    public async Task UpdateTerrain_StoresAFlagValueAndKeepsItThroughOtherChanges()
    {
        byte[] trailing = [2, 9, 8, 7];
        var store = Store(TerrainRegion([.. new byte[16384], .. trailing]));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var flagged = await client.PostAsJsonAsync(TerrainUrl, new { tiles = new object[] { new { plane = 1, x = 2, y = 3, flags = 5 } } });
        using var raised = await client.PostAsJsonAsync(TerrainUrl, new { tiles = new object[] { new { plane = 1, x = 2, y = 3, height = 8 } } });

        flagged.EnsureSuccessStatusCode();
        raised.EnsureSuccessStatusCode();
        using var render = await client.PostAsJsonAsync("/api/v1/cache/types/maps/258/render", new { });
        var planes = (await render.Content.ReadFromJsonAsync<MapRenderRegion>())!.Planes;
        Assert.AreEqual(5, planes[1].Flags[2 * 64 + 3]);
        Assert.AreEqual(1, planes.Take(4).Sum(plane => plane.Flags.Count(flags => flags != 0)));
        CollectionAssert.AreEqual(trailing, Stored(store, 0)[^4..]);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(33)]
    public async Task UpdateTerrain_RejectsAFlagValueWithNoStoredFormWithoutWriting(int flags)
    {
        var store = Store(TerrainRegion(new byte[16384]));
        var before = Stored(store, 0);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(TerrainUrl, new { tiles = new object[] { new { plane = 0, x = 1, y = 1, underlay = 3 }, new { plane = 0, x = 0, y = 0, flags } } });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "between 0 and 32");
        CollectionAssert.AreEqual(before, Stored(store, 0));
    }

    [TestMethod]
    public async Task UpdateTerrain_WritesNothingWithoutEffectiveChanges()
    {
        var store = Store(TerrainRegion(new byte[16384]));
        var before = Stored(store, 0);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(TerrainUrl, new { tiles = Array.Empty<object>() });

        response.EnsureSuccessStatusCode();
        CollectionAssert.AreEqual(before, Stored(store, 0));
    }

    [TestMethod]
    [DataRow("{\"plane\":4,\"x\":0,\"y\":0}")]
    [DataRow("{\"plane\":-1,\"x\":0,\"y\":0}")]
    [DataRow("{\"plane\":0,\"x\":64,\"y\":0}")]
    [DataRow("{\"plane\":0,\"x\":0,\"y\":64}")]
    [DataRow("{\"plane\":0,\"x\":0,\"y\":0,\"height\":256}")]
    [DataRow("{\"plane\":0,\"x\":0,\"y\":0,\"height\":1}")]
    [DataRow("{\"plane\":0,\"x\":0,\"y\":0,\"underlay\":175}")]
    [DataRow("{\"plane\":0,\"x\":0,\"y\":0,\"overlay\":256}")]
    [DataRow("{\"plane\":0,\"x\":0,\"y\":0,\"overlayShape\":12}")]
    [DataRow("{\"plane\":0,\"x\":0,\"y\":0,\"overlayRotation\":4}")]
    public async Task UpdateTerrain_RejectsOutOfRangeChangesWithoutWriting(string tile)
    {
        var store = Store(TerrainRegion(new byte[16384]));
        var before = Stored(store, 0);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var body = new StringContent($"{{\"tiles\":[{tile}]}}", System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(TerrainUrl, body);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        CollectionAssert.AreEqual(before, Stored(store, 0));
    }

    [TestMethod]
    public async Task UpdateTerrain_RejectsAnInvalidRegionAndMissingList()
    {
        var store = Store(TerrainRegion(new byte[16384]));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var region = await client.PostAsJsonAsync("/api/v1/cache/types/maps/65536/terrain", new { tiles = Array.Empty<object>() });
        using var missing = await client.PostAsJsonAsync(TerrainUrl, new { });

        Assert.AreEqual(HttpStatusCode.BadRequest, region.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, missing.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTerrain_ReportsARegionWithoutTerrain()
    {
        var store = Store(("l1_2", Landscape()));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(TerrainUrl, new { tiles = Array.Empty<object>() });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateTerrain_RefusesUnreadableTerrainWithoutWriting()
    {
        var store = Store(TerrainRegion([51]));
        var before = Stored(store, 0);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(TerrainUrl, new { tiles = new object[] { new { plane = 0, x = 0, y = 0, height = 8 } } });

        Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        CollectionAssert.AreEqual(before, Stored(store, 0));
    }

    [TestMethod]
    public async Task Floors_ListsUnderlaysAndOverlaysWithTheirValues()
    {
        var store = Store(("m1_2", new byte[16384]));
        SeedFloors(store);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        var floors = await client.GetFromJsonAsync<FloorRenderDefinition[]>("/api/v1/cache/types/maps/floors");

        var underlay = floors!.Single(floor => !floor.Overlay);
        Assert.AreEqual(1, underlay.Id);
        Assert.AreEqual(0x102030, underlay.Rgb);
        var overlay = floors.Single(floor => floor.Overlay);
        Assert.AreEqual(1, overlay.Id);
        Assert.AreEqual(0x405060, overlay.Rgb);
    }

    private static void SeedFloors(MemoryFileStore store)
    {
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        foreach (var (group, definition) in new[] { (1, new byte[] { 1, 0x10, 0x20, 0x30, 0 }), (4, new byte[] { 1, 0x40, 0x50, 0x60, 0 }) })
        {
            var archive = new ReferenceTableEntry(group);
            archive.InitializeEntries(1);
            archive.AddEntry(0, new ReferenceTableChildEntry(0));
            table.AddEntry(group, archive);
            store.Set(2, group, ContainerBytes(definition));
        }
        using var encoded = new ReferenceTableCodec().Encode(table);
        store.Set(255, 2, ContainerBytes(encoded.ToArray()));
    }

    [TestMethod]
    public async Task ReplaceLandscape_StoresPlacementsAndLeavesOtherArchivesUnchanged()
    {
        var store = Store(EditableRegion(Landscape(new MapRenderPlacement(1, 10, 0, 1, 1, 0))));
        SeedObjectIndex(store);
        var terrain = Stored(store, 0);
        var neighbour = Stored(store, 5);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        MapRenderPlacement[] saved = [new(300, 22, 3, 63, 62, 3), new(7, 10, 1, 4, 5, 1)];

        using var response = await client.PostAsJsonAsync(LandscapeUrl, new { objects = saved });

        response.EnsureSuccessStatusCode();
        using var render = await client.PostAsJsonAsync("/api/v1/cache/types/maps/258/render", new { });
        var region = await render.Content.ReadFromJsonAsync<MapRenderRegion>();
        CollectionAssert.AreEqual(new[] { saved[1], saved[0] }, region!.Objects.ToArray());
        CollectionAssert.AreEqual(terrain, Stored(store, 0));
        CollectionAssert.AreEqual(neighbour, Stored(store, 5));
    }

    [TestMethod]
    public async Task ReplaceLandscape_AcceptsAnEmptyListToRemoveEveryPlacement()
    {
        var store = Store(EditableRegion(Landscape(new MapRenderPlacement(1, 10, 0, 1, 1, 0))));
        SeedObjectIndex(store);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(LandscapeUrl, new { objects = Array.Empty<MapRenderPlacement>() });

        response.EnsureSuccessStatusCode();
        using var render = await client.PostAsJsonAsync("/api/v1/cache/types/maps/258/render", new { });
        Assert.IsEmpty((await render.Content.ReadFromJsonAsync<MapRenderRegion>())!.Objects);
    }

    [TestMethod]
    [DataRow(512, 10, 0, 1, 1, 0)]
    [DataRow(-1, 10, 0, 1, 1, 0)]
    [DataRow(1, 23, 0, 1, 1, 0)]
    [DataRow(1, 10, 4, 1, 1, 0)]
    [DataRow(1, 10, 0, 64, 1, 0)]
    [DataRow(1, 10, 0, 1, 64, 0)]
    [DataRow(1, 10, 0, 1, 1, 4)]
    public async Task ReplaceLandscape_RejectsOutOfRangePlacementsWithoutWriting(int id, int shape, int rotation, int x, int y, int plane)
    {
        var store = Store(EditableRegion(Landscape(new MapRenderPlacement(1, 10, 0, 1, 1, 0))));
        SeedObjectIndex(store);
        var before = Stored(store, 1);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(LandscapeUrl, new { objects = new[] { new MapRenderPlacement(id, shape, rotation, x, y, plane) } });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        CollectionAssert.AreEqual(before, Stored(store, 1));
    }

    [TestMethod]
    public async Task ReplaceLandscape_RejectsAnInvalidRegionAndMissingList()
    {
        var store = Store(EditableRegion(Landscape()));
        SeedObjectIndex(store);
        var before = Stored(store, 1);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var region = await client.PostAsJsonAsync("/api/v1/cache/types/maps/65536/landscape", new { objects = Array.Empty<MapRenderPlacement>() });
        using var missing = await client.PostAsJsonAsync(LandscapeUrl, new { });

        Assert.AreEqual(HttpStatusCode.BadRequest, region.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, missing.StatusCode);
        CollectionAssert.AreEqual(before, Stored(store, 1));
    }

    [TestMethod]
    public async Task ReplaceLandscape_ReportsARegionWithoutALandscape()
    {
        var store = Store(("m1_2", new byte[16384]));
        SeedObjectIndex(store);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(LandscapeUrl, new { objects = Array.Empty<MapRenderPlacement>() });

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task ReplaceLandscape_RefusesAnUnreadableLandscapeWithoutWriting()
    {
        var store = Store(EditableRegion([1, 128]));
        SeedObjectIndex(store);
        var before = Stored(store, 1);
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.PostAsJsonAsync(LandscapeUrl, new { objects = Array.Empty<MapRenderPlacement>() });

        Assert.AreEqual(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        CollectionAssert.AreEqual(before, Stored(store, 1));
    }

    [TestMethod]
    public async Task RenderRegion_AddsUnderwaterDepthsToSurfaceHeightsAndReadsUnencryptedObjects()
    {
        var data = new byte[16384];
        var depths = new byte[4098];
        depths[0] = 1;
        depths[1] = 3;
        var store = Store(("m1_2", data), ("m2_2", data), ("m1_3", data), ("m2_3", data), ("um1_2", depths), ("ul1_2", new byte[] { 1, 1, 40, 0, 0 }));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/maps/258/render", new { });
        response.EnsureSuccessStatusCode();
        var region = await response.Content.ReadFromJsonAsync<MapRenderRegion>();
        Assert.IsNotNull(region?.UnderwaterPlane);
        Assert.AreEqual(region.Planes[0].CornerHeights[0] + 96, region.UnderwaterPlane.CornerHeights[0]);
        Assert.AreEqual(region.Planes[0].CornerHeights[64 * 65 + 64], region.UnderwaterPlane.CornerHeights[64 * 65 + 64]);
        Assert.HasCount(1, region.UnderwaterObjects!);
        Assert.AreEqual(10, region.UnderwaterObjects![0].Shape);
    }

    [TestMethod]
    public async Task RenderRegion_UsesAuthoritativeNeighborBordersAndIndependentLandscapeStatus()
    {
        var data = new byte[16384];
        byte[] lighting = [0, 1, 0, 0xdd, 0xce, 0xbb, 1, 1, 0, 0, 64, 0, 64, 0, 32, 0, 0, 1, 0xab, 0xcd, 0];
        var store = Store(("m1_2", [..data, ..lighting]), ("m2_2", data), ("m1_3", data), ("m2_3", data), ("m3_2", data), ("m3_3", data), ("l1_2", new byte[] { 1, 128 }));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapTypeEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/maps/258/render", new { });
        response.EnsureSuccessStatusCode();
        var region = await response.Content.ReadFromJsonAsync<MapRenderRegion>();
        Assert.IsNotNull(region);
        Assert.AreEqual("available", region.TerrainStatus);
        Assert.AreEqual(0xddcebb, region.Atmosphere?.LightRgb);
        Assert.AreEqual(0xc8c0a8, region.Atmosphere?.SkyRgb);
        Assert.AreEqual(0, region.Atmosphere?.FogValue);
        Assert.IsNull(region.Atmosphere?.EnvironmentFaces);
        Assert.HasCount(1, region.PointLights!);
        Assert.AreEqual(0xabcd, region.PointLights![0].PackedColor);
        Assert.AreEqual(128, region.PointLights[0].Height);
        Assert.AreEqual("failed", region.LandscapeStatus);
        Assert.HasCount(4, region.Planes);
        Assert.IsEmpty(region.Objects);
        Assert.IsTrue(region.Warnings.Any(warning => warning.Contains("XTEA", StringComparison.Ordinal)));
        using var nextResponse = await client.PostAsJsonAsync("/api/v1/cache/types/maps/514/render", new { xteaKeys = new[] { 0, 0, 0, 0 } });
        nextResponse.EnsureSuccessStatusCode();
        var neighbor = await nextResponse.Content.ReadFromJsonAsync<MapRenderRegion>();
        Assert.IsNotNull(neighbor);
        Assert.IsNull(neighbor.Atmosphere);
        Assert.IsNull(neighbor.PointLights);
        for (var plane = 0; plane < 4; plane++)
        for (var y = 0; y <= 64; y++)
            Assert.AreEqual(neighbor.Planes[plane].CornerHeights[y], region.Planes[plane].CornerHeights[64 * 65 + y]);
    }

    [TestMethod]
    public async Task RenderRegion_ReportsAbsentFilesAndIncompleteBoundaryWithoutGeneratedHeights()
    {
        await using var app = await CacheServiceTestHost.StartAsync(Store(("m255_255", [..new byte[16384], 1, 1, 0])), group => group.MapTypeEndpoints());
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
        Assert.IsNull(edge.PointLights);
        Assert.IsTrue(edge.Warnings.Any(warning => warning.Contains("lighting data is truncated", StringComparison.Ordinal)));
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
        CollectionAssert.AreEqual(new[] { new FloorRenderDefinition(6, false, 0x123456, -1, null, 512, null) }, region.Floors.ToArray());
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
