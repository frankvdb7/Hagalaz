using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class WorldMapTests
{
    private const byte Empty = 62 << 2;

    private static WorldMapTile Floor(int underlay) => new([new WorldMapLayer(underlay, 0, 0, [])]);

    private static byte[] Area(params (int X, int Y, byte[] Tiles)[] blocks)
    {
        using var stream = new MemoryStream();
        stream.Write([2, 7, 9, 1, 40]);
        foreach (var (x, y, tiles) in blocks)
        {
            stream.Write([0, (byte)x, (byte)y]);
            stream.Write(tiles);
        }
        return stream.ToArray();
    }

    private static byte[] EmptyTiles() => Enumerable.Repeat(Empty, 4096).ToArray();

    private static WorldMapTile[] RoundTrip(WorldMapTile[] tiles)
    {
        var data = Area((1, 2, EmptyTiles()));
        var area = WorldMapAreaCodec.Read(data);
        var patched = WorldMapAreaCodec.ReplaceRegions(data, area, new Dictionary<WorldMapBlock, WorldMapTile[]> { [area.Blocks[0]] = tiles });
        var updated = WorldMapAreaCodec.Read(patched);
        return WorldMapAreaCodec.DecodeRegion(patched, updated, updated.Blocks[0]);
    }

    private static void AssertSame(WorldMapTile expected, WorldMapTile actual)
    {
        Assert.Equal(expected.Layers.Count, actual.Layers.Count);
        foreach (var (left, right) in expected.Layers.Zip(actual.Layers))
        {
            Assert.Equal((left.Underlay, left.Overlay, left.Info), (right.Underlay, right.Overlay, right.Info));
            Assert.Equal(left.Objects, right.Objects);
        }
    }

    [Fact]
    public void Read_ReadsPalettesAndEveryTileForm()
    {
        byte[] tiles = [.. EmptyTiles()];
        // Palette underlay, literal underlay, palette overlay with underlay, then a two-layer tile with objects.
        byte[] head = [1 << 2, 63 << 2, 200, (0 << 2) | 2, 33, 1 | (1 << 1) | 8 | 16, 5, 40, 65, 1, 0, 9, 137, 6, 0, 0, 2, 0x80, 1, 0, 0, 7, 0x7f, 0xff, 0];
        var body = head.Concat(tiles.Skip(4)).ToArray();
        var data = Area((3, 4, body));

        var area = WorldMapAreaCodec.Read(data);
        Assert.Equal(new[] { 7, 9 }, area.Underlays);
        Assert.Equal(new[] { 40 }, area.Overlays);
        Assert.Equal(data.Length, area.Blocks[0].End);
        var decoded = WorldMapAreaCodec.DecodeRegion(data, area, area.Blocks[0]);

        AssertSame(Floor(9), decoded[0]);
        AssertSame(Floor(200), decoded[1]);
        AssertSame(new WorldMapTile([new WorldMapLayer(33, 40, 0, [])]), decoded[2]);
        AssertSame(new WorldMapTile([
            new WorldMapLayer(5, 40, 65, [new WorldMapObject(9, 137)]),
            new WorldMapLayer(6, 0, 0, [new WorldMapObject(65536, 7), new WorldMapObject(-1, 0)]),
        ]), decoded[3]);
        Assert.Empty(decoded[4].Layers);
    }

    [Fact]
    public void Read_RejectsATruncatedBlock()
    {
        Assert.Throws<EndOfStreamException>(() => WorldMapAreaCodec.Read(Area((1, 1, [Empty]))));
    }

    [Fact]
    public void ReplaceRegions_RoundTripsEveryTileShape()
    {
        var tiles = Enumerable.Repeat(WorldMapTile.Empty, 4096).ToArray();
        tiles[0] = Floor(7);
        tiles[1] = Floor(201);
        tiles[2] = new WorldMapTile([new WorldMapLayer(33, 40, 0, [])]);
        tiles[3] = new WorldMapTile([new WorldMapLayer(33, 77, 0, [])]);
        tiles[4] = new WorldMapTile([new WorldMapLayer(5, 40, 65, [])]);
        tiles[5] = new WorldMapTile([new WorldMapLayer(5, 0, 0, [new WorldMapObject(70000, 137), new WorldMapObject(12, 0)])]);
        tiles[6] = new WorldMapTile([new WorldMapLayer(1, 0, 0, []), new WorldMapLayer(2, 0, 0, []), new WorldMapLayer(3, 0, 0, []), new WorldMapLayer(4, 9, 3, [new WorldMapObject(32767, 1)])]);

        var decoded = RoundTrip(tiles);

        for (var tile = 0; tile < 8; tile++) AssertSame(tiles[tile], decoded[tile]);
    }

    [Fact]
    public void ReplaceRegions_KeepsOtherBlocksByteForByte()
    {
        var other = EmptyTiles();
        other[10] = 1 << 2;
        var data = Area((1, 2, EmptyTiles()), (3, 4, other));
        var area = WorldMapAreaCodec.Read(data);
        var tiles = Enumerable.Repeat(Floor(55), 4096).ToArray();

        var patched = WorldMapAreaCodec.ReplaceRegions(data, area, new Dictionary<WorldMapBlock, WorldMapTile[]> { [area.Blocks[0]] = tiles });

        var updated = WorldMapAreaCodec.Read(patched);
        Assert.Equal(data[area.Blocks[1].Start..area.Blocks[1].End], patched[updated.Blocks[1].Start..updated.Blocks[1].End]);
    }

    [Fact]
    public void ReadDetails_ReadsTheMapNameAndParts()
    {
        byte[] details = [.. "main"u8, 0, .. "Main map"u8, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 100, 0, 2,
            0, 8, 0, 9, 192, 9, 63, 10, 255, 8, 0, 9, 192, 9, 63, 10, 255,
            0, 6, 192, 12, 0, 7, 255, 12, 255, 8, 0, 10, 192, 9, 63, 11, 191];

        var (map, parts) = WorldMapAreaCodec.ReadDetails(details);

        Assert.Equal("main", map);
        Assert.Equal(2, parts.Count);
        Assert.Equal(new WorldMapAreaPart(0, 1728, 3072, 2047, 3327, 2048, 2752, 2367, 3007), parts[1]);
        Assert.True(parts[1].ContainsReal(1728, 3072));
        Assert.False(parts[1].ContainsReal(2048, 2752));
    }

    private static TerrainRenderPlane[] Planes() =>
        Enumerable.Range(0, 4).Select(_ => new TerrainRenderPlane(new int[4096], new int[4096], new int[4096], new int[4096], new int[4096], new int[4096])).ToArray();

    private static ObjectType Definition(int id, int mapSprite = -1, int mapIcon = -1) => new(id) { MapSpriteType = mapSprite, MapIcon = mapIcon };

    [Theory]
    [InlineData(0, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(9, true)]
    [InlineData(1, false)]
    [InlineData(4, false)]
    [InlineData(10, false)]
    [InlineData(12, false)]
    [InlineData(22, false)]
    public void Includes_KeepsWallsAndNotPlainObjects(int shape, bool expected)
    {
        Assert.Equal(expected, WorldMapTileGenerator.Includes(new MapRenderPlacement(1, shape, 0, 0, 0, 0), Definition(1)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(22)]
    public void Includes_KeepsObjectsWithAMapSpriteOrIcon(int shape)
    {
        var placement = new MapRenderPlacement(1, shape, 0, 0, 0, 0);
        Assert.True(WorldMapTileGenerator.Includes(placement, Definition(1, mapSprite: 4)));
        Assert.True(WorldMapTileGenerator.Includes(placement, Definition(1, mapIcon: 4)));
    }

    [Fact]
    public void Includes_LeavesOutDecorationsAndUnknownObjects()
    {
        Assert.False(WorldMapTileGenerator.Includes(new MapRenderPlacement(1, 4, 0, 0, 0, 0), Definition(1, mapSprite: 4)));
        Assert.False(WorldMapTileGenerator.Includes(new MapRenderPlacement(1, 0, 0, 0, 0, 0), null));
    }

    [Fact]
    public void Generate_CarriesPlaneZeroFloorsAndPacksObjects()
    {
        var planes = Planes();
        var tile = 2 * 64 + 3;
        planes[0].Underlays[tile] = 5;
        planes[0].Overlays[tile] = 9;
        planes[0].OverlayShapes[tile] = 2;
        planes[0].OverlayRotations[tile] = 3;

        var tiles = WorldMapTileGenerator.Generate(planes, [new MapRenderPlacement(7, 9, 2, 2, 3, 0), new MapRenderPlacement(8, 22, 0, 2, 3, 0)], id => Definition(id));

        var layer = Assert.Single(tiles[tile].Layers);
        Assert.Equal((5, 9, 3 << 6 | 2), (layer.Underlay, layer.Overlay, layer.Info));
        Assert.Equal(new WorldMapObject(7, 2 << 6 | 9), Assert.Single(layer.Objects));
    }

    [Theory]
    [InlineData(8, 4, false, 2)]
    [InlineData(2, 4, false, 2)]
    [InlineData(8, 0, true, 2)]
    [InlineData(8, 0, false, 1)]
    [InlineData(0, 4, true, 1)]
    public void Generate_AddsAnUpperLayerOnlyWhenFlaggedWithContent(int flags, int underlay, bool wall, int expectedLayers)
    {
        var planes = Planes();
        planes[1].Flags[0] = flags;
        planes[1].Underlays[0] = underlay;
        MapRenderPlacement[] placements = wall ? [new MapRenderPlacement(7, 0, 0, 0, 0, 1)] : [];

        var tiles = WorldMapTileGenerator.Generate(planes, placements, id => Definition(id));

        Assert.Equal(expectedLayers, tiles[0].Layers.Count);
    }
}
