using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Maps;
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
        var map = new FakeMaps();
        var refresher = new MapWorldMapRefresher(map.Cache.Object, new RegionMapArchive(map.Cache.Object), _ => null);
        var interleaved = false;
        map.SetRegion(West, 10, underlay: 4);
        map.SetRegion(East, 11, underlay: 6);
        // The east refresh is written after the west one read `main` but before it rewrites it.
        map.BeforeReplace = () =>
        {
            if (interleaved) return;
            interleaved = true;
            Assert.Equal([East], refresher.Refresh([East]).Refreshed);
        };

        Assert.Equal([West], refresher.Refresh([West]).Refreshed);

        var area = WorldMapAreaCodec.Read(map.Main);
        Assert.Equal(4, Underlay(map.Main, area, 0));
        Assert.Equal(6, Underlay(map.Main, area, 1));
    }

    [Fact]
    public void Refresh_RegeneratesARegionSavedAndRefreshedWhileThisRefreshWasGenerating()
    {
        var map = new FakeMaps();
        var refresher = new MapWorldMapRefresher(map.Cache.Object, new RegionMapArchive(map.Cache.Object), _ => null);
        var interleaved = false;
        map.SetRegion(West, 10, underlay: 4);
        // A save of the west terrain and its own refresh both land after this refresh generated the old tiles.
        map.BeforeReplace = () =>
        {
            if (interleaved) return;
            interleaved = true;
            map.SetRegion(West, 10, underlay: 5);
            Assert.Equal([West], refresher.Refresh([West]).Refreshed);
        };

        // The save's own refresh already wrote the new tiles, so this one finds them in place.
        Assert.Equal([West], refresher.Refresh([West]).Unchanged);

        Assert.Equal(5, Underlay(map.Main, WorldMapAreaCodec.Read(map.Main), 0));
    }

    [Fact]
    public void Refresh_SkipsARegionInsideTheMapPartWithoutReadingItsFilesWhenMainHasNoBlockForIt()
    {
        var map = new FakeMaps { Main = Area((32, 39)) };
        map.SetRegion(East, 11, underlay: 6);

        var result = new MapWorldMapRefresher(map.Cache.Object, new RegionMapArchive(map.Cache.Object), _ => null).Refresh([East]);

        Assert.Equal([new MapWorldMapSkip(East, "Region is not on the main world map.")], result.Skipped);
        map.Cache.Verify(api => api.ReadContainer(5, 11), Times.Never);
    }

    private static int Underlay(byte[] main, WorldMapAreaFile area, int block) =>
        WorldMapAreaCodec.DecodeRegion(main, area, area.Blocks[block])[0].Layers[0].Underlay;

    /// <summary>A strict cache holding the main world map and the region files written to it.</summary>
    private sealed class FakeMaps
    {
        private readonly Dictionary<string, (int File, int Version)> _files = new();

        public FakeMaps()
        {
            Cache.Setup(api => api.GetFileId(23, "main")).Returns(MainFile);
            Cache.Setup(api => api.GetFileId(23, "details")).Returns(DetailsFile);
            Cache.Setup(api => api.ReadArchive(23, DetailsFile)).Returns(() =>
            {
                var archive = new Archive(1);
                archive.Entries![0] = new MemoryStream(Details);
                return archive;
            });
            Cache.Setup(api => api.ReadReferenceTable(5)).Returns(() => Table());
            Cache.Setup(api => api.ReadContainer(23, MainFile)).Returns(() => new Container(CompressionType.Gzip, new MemoryStream(Main)));
            Cache.Setup(api => api.TryReplaceContainer(23, MainFile, It.IsAny<byte[]>(), It.IsAny<IContainer>()))
                .Returns((int _, int _, byte[] expected, IContainer replacement) =>
                {
                    BeforeReplace();
                    if (!expected.AsSpan().SequenceEqual(Main)) return false;
                    Main = replacement.Data.ToArray();
                    return true;
                });
        }

        public Mock<ICacheAPI> Cache { get; } = new(MockBehavior.Strict);
        public byte[] Main { get; set; } = Area((32, 39), (33, 39));
        public Action BeforeReplace { get; set; } = () => { };

        /// <summary>Stores a region's terrain, bumping its version the way a cache write does.</summary>
        public void SetRegion(int region, int terrainFile, int underlay)
        {
            var (x, y) = (region >> 8, region & 255);
            var name = $"m{x}_{y}";
            var terrain = MapTerrainPatcher.Patch(new byte[4 * 64 * 64], [new MapTerrainTileChange(0, 0, 0, Underlay: underlay)]);
            _files[name] = (terrainFile, _files.GetValueOrDefault(name).Version + 1);
            Cache.Setup(api => api.GetFileId(5, name)).Returns(terrainFile);
            Cache.Setup(api => api.GetFileId(5, $"l{x}_{y}")).Returns(-1);
            Cache.Setup(api => api.ReadContainer(5, terrainFile)).Returns(() => new Container(new MemoryStream(terrain)));
        }

        private IReferenceTable Table()
        {
            var versions = _files.Values.ToDictionary(file => file.File, file => file.Version);
            var table = new Mock<IReferenceTable>();
            table.Setup(t => t.GetFileId(It.IsAny<string>())).Returns((string name) => _files.TryGetValue(name, out var file) ? file.File : -1);
            table.Setup(t => t.GetEntry(It.IsAny<int>())).Returns((int file) =>
                versions.TryGetValue(file, out var version) ? Mock.Of<IReferenceTableEntry>(entry => entry.Version == version) : null);
            return table.Object;
        }
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
