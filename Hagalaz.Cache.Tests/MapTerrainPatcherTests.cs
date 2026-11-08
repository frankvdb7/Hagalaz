using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class MapTerrainPatcherTests
{
    private const int Region = 12850;

    private static byte[] Fixture() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "maps", "terrain-12850.dat"));

    /// <summary>Every tile automatic, except tiles given explicit tokens; the first listed tile is plane 0, 0,0.</summary>
    private static byte[] Terrain(byte[] firstTile, params byte[] trailing) =>
        [.. firstTile, .. new byte[4 * 64 * 64 - 1], .. trailing];

    private static TerrainRenderPlane[] Decode(byte[] terrain) => MapRenderDecoder.DecodeTerrain(new MemoryStream(terrain), Region);

    [Fact]
    public void EmptyPatch_ReproducesARealRegionExactly()
    {
        var stored = Fixture();
        Assert.Equal(stored, MapTerrainPatcher.Patch(stored, []));
    }

    [Fact]
    public void Patch_ChangesOnlyTheEditedTileOfARealRegion()
    {
        var stored = Fixture();
        var before = Decode(stored);
        var patched = MapTerrainPatcher.Patch(stored, [new MapTerrainTileChange(0, 10, 20, Height: 8, Underlay: 3, Overlay: 7, OverlayShape: 2, OverlayRotation: 1)]);
        var after = Decode(patched);

        var tile = 10 * 64 + 20;
        Assert.Equal(-256, after[0].Heights[tile]);
        Assert.Equal(3, after[0].Underlays[tile]);
        Assert.Equal(7, after[0].Overlays[tile]);
        Assert.Equal(2, after[0].OverlayShapes[tile]);
        Assert.Equal(1, after[0].OverlayRotations[tile]);
        Assert.Equal(before[0].Flags[tile], after[0].Flags[tile]);
        for (var other = 0; other < 4096; other++)
        {
            if (other == tile) continue;
            Assert.Equal(before[0].Heights[other], after[0].Heights[other]);
            Assert.Equal(before[0].Underlays[other], after[0].Underlays[other]);
            Assert.Equal(before[0].Overlays[other], after[0].Overlays[other]);
        }
        Assert.Equal(stored.AsSpan(MapTerrainPatcher.TerrainEnd(stored)).ToArray(), patched.AsSpan(MapTerrainPatcher.TerrainEnd(patched)).ToArray());
    }

    [Fact]
    public void Patch_KeepsFlagsAutomaticHeightsAndTrailingRecords()
    {
        byte[] trailing = [2, 9, 8, 7];
        var stored = Terrain([51, 0], trailing);

        var patched = MapTerrainPatcher.Patch(stored, [new MapTerrainTileChange(0, 0, 0, Underlay: 5)]);

        var after = Decode(patched);
        Assert.Equal(2, after[0].Flags[0]);
        Assert.Equal(5, after[0].Underlays[0]);
        Assert.Equal(Decode(stored)[0].Heights[0], after[0].Heights[0]);
        Assert.Equal(Decode(stored)[0].Heights[1], after[0].Heights[1]);
        Assert.Equal(trailing, patched[^4..]);
    }

    [Fact]
    public void Patch_ClearsAndReplacesRepeatedFloorTokens()
    {
        var stored = Terrain([90, 95, 6, 11, 51, 1, 8]);

        var cleared = Decode(MapTerrainPatcher.Patch(stored, [new MapTerrainTileChange(0, 0, 0, Underlay: 0, Overlay: 0)]));
        Assert.Equal(0, cleared[0].Underlays[0]);
        Assert.Equal(0, cleared[0].Overlays[0]);
        Assert.Equal(2, cleared[0].Flags[0]);
        Assert.Equal(-256, cleared[0].Heights[0]);

        var replaced = Decode(MapTerrainPatcher.Patch(stored, [new MapTerrainTileChange(0, 0, 0, Overlay: 200, OverlayShape: 11, OverlayRotation: 3)]));
        Assert.Equal(200, replaced[0].Overlays[0]);
        Assert.Equal(11, replaced[0].OverlayShapes[0]);
        Assert.Equal(3, replaced[0].OverlayRotations[0]);
        Assert.Equal(14, replaced[0].Underlays[0]);
    }

    [Fact]
    public void Patch_IsIdempotent()
    {
        var change = new MapTerrainTileChange(1, 3, 4, Height: 2, Underlay: 9, Overlay: 4);
        var once = MapTerrainPatcher.Patch(Fixture(), [change]);
        Assert.Equal(once, MapTerrainPatcher.Patch(once, [change]));
    }

    [Fact]
    public void Patch_RaisingAPlaneCarriesThePlanesAboveIt()
    {
        var stored = Terrain([0]);
        var after = Decode(MapTerrainPatcher.Patch(stored, [new MapTerrainTileChange(0, 0, 0, Height: 10)]));
        Assert.Equal(-320, after[0].Heights[0]);
        Assert.Equal(-320 - 960, after[1].Heights[0]);
    }

    [Fact]
    public void Patch_RefusesTruncatedTerrain()
    {
        Assert.Throws<EndOfStreamException>(() => MapTerrainPatcher.Patch([51], []));
    }
}
