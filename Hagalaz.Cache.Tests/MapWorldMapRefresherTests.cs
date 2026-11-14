using System;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Providers;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class MapWorldMapRefresherTests
{
    private const int MainFile = 1;
    private const int DetailsFile = 2;
    private const int West = 32 << 8 | 39;
    private const int East = 33 << 8 | 39;

    // One main-map part whose display and real coordinates are 2048,2496 to 2367,2815 (regions 32-36, 39-43).
    private static readonly byte[] Details = [.. "main"u8, 0, .. "Main map"u8, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 100, 0, 1,
        0, 8, 0, 9, 192, 9, 63, 10, 255, 8, 0, 9, 192, 9, 63, 10, 255];

    [Fact]
    public void Refresh_KeepsARefreshOfAnotherRegionThatLandsWhileThisOneIsGenerating()
    {
        var main = Area((32, 39), (33, 39));
        var cache = new Mock<ICacheAPI>(MockBehavior.Strict);
        var refresher = new MapWorldMapRefresher(cache.Object, _ => null);
        var interleaved = false;
        cache.Setup(api => api.GetFileId(23, "main")).Returns(MainFile);
        cache.Setup(api => api.GetFileId(23, "details")).Returns(DetailsFile);
        cache.Setup(api => api.ReadArchive(23, DetailsFile)).Returns(() =>
        {
            var archive = new Archive(1);
            archive.Entries![0] = new MemoryStream(Details);
            return archive;
        });
        SetupRegion(cache, West, 10, underlay: 4);
        SetupRegion(cache, East, 11, underlay: 6);
        cache.Setup(api => api.UpdateContainer(23, MainFile, It.IsAny<Func<IContainer, IContainer?>>()))
            .Returns((int _, int _, Func<IContainer, IContainer?> change) =>
            {
                // The east refresh is written after the west one read its region files but before it rewrites `main`.
                if (!interleaved)
                {
                    interleaved = true;
                    Assert.Equal([East], refresher.Refresh([East]).Refreshed);
                }
                using var current = new Container(CompressionType.Gzip, new MemoryStream(main));
                using var updated = change(current);
                if (updated is null) return false;
                main = updated.Data.ToArray();
                return true;
            });

        Assert.Equal([West], refresher.Refresh([West]).Refreshed);

        var area = WorldMapAreaCodec.Read(main);
        Assert.Equal(4, Underlay(main, area, 0));
        Assert.Equal(6, Underlay(main, area, 1));
    }

    private static int Underlay(byte[] main, WorldMapAreaFile area, int block) =>
        WorldMapAreaCodec.DecodeRegion(main, area, area.Blocks[block])[0].Layers[0].Underlay;

    private static void SetupRegion(Mock<ICacheAPI> cache, int region, int terrainFile, int underlay)
    {
        var (x, y) = (region >> 8, region & 255);
        var terrain = MapTerrainPatcher.Patch(new byte[4 * 64 * 64], [new MapTerrainTileChange(0, 0, 0, Underlay: underlay)]);
        cache.Setup(api => api.GetFileId(5, $"m{x}_{y}")).Returns(terrainFile);
        cache.Setup(api => api.GetFileId(5, $"l{x}_{y}")).Returns(-1);
        cache.Setup(api => api.ReadContainer(5, terrainFile)).Returns(() => new Container(new MemoryStream(terrain)));
    }

    private static byte[] Area(params (int X, int Y)[] blocks)
    {
        using var stream = new MemoryStream();
        stream.Write([2, 7, 9, 1, 40]);
        foreach (var (x, y) in blocks)
        {
            stream.Write([0, (byte)x, (byte)y]);
            stream.Write(Enumerable.Repeat((byte)(62 << 2), 4096).ToArray());
        }
        return stream.ToArray();
    }
}
