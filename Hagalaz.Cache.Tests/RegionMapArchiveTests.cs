using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Maps;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class RegionMapArchiveTests
{
    private const int Region = 12850;
    private static readonly byte[] StoredTerrain = Fixture("terrain-12850.dat");
    private static readonly byte[] StoredLandscape = Fixture("landscape-12850.dat");

    [Theory]
    [InlineData(RegionMapFile.Terrain, "m50_50")]
    [InlineData(RegionMapFile.Landscape, "l50_50")]
    [InlineData(RegionMapFile.UnderwaterTerrain, "um50_50")]
    [InlineData(RegionMapFile.UnderwaterLandscape, "ul50_50")]
    public void Locate_FindsEachKindByItsStoredName(RegionMapFile kind, string name)
    {
        var index = new FakeIndex();
        index.Store(name, [1]);

        Assert.Equal(index.FileOf(name), new RegionMapArchive(index.Cache.Object).Locate(Region, kind));
        Assert.Null(new RegionMapArchive(index.Cache.Object).Locate(Region + 1, kind));
    }

    [Fact]
    public void ListRegions_ListsRegionsWithTerrainOrLandscapeFromOneTableRead()
    {
        var index = new FakeIndex();
        index.Store("m50_50", [1]);
        index.Store("l50_50", [1]);
        index.Store("m50_51", [1]);
        index.Store("um50_52", [1]);
        index.Store("n50_53", [1]);

        var regions = new RegionMapArchive(index.Cache.Object).ListRegions().Value;

        Assert.Equal(
            [new RegionMapEntry(Region, 50, 50, index.FileOf("m50_50"), index.FileOf("l50_50")), new RegionMapEntry(Region + 1, 50, 51, index.FileOf("m50_51"), null)],
            regions);
        Assert.Equal(1, index.TableReads);
    }

    [Fact]
    public void ListRegions_FailsWithoutNamedReferenceMetadata()
    {
        var index = new FakeIndex { Named = false };

        Assert.True(new RegionMapArchive(index.Cache.Object).ListRegions().HasError<RegionMapNamesMissingError>());
        Assert.False(new RegionMapArchive(index.Cache.Object).HasNamedFiles);
    }

    [Fact]
    public void Revisions_ReadsTheVersionsOfSeveralRegionsFromOneTableRead()
    {
        var index = new FakeIndex();
        index.Store("m50_50", [1]);
        index.Store("m50_50", [2]);
        index.Store("l50_50", [1]);
        index.Store("m50_51", [1]);

        var revisions = new RegionMapArchive(index.Cache.Object).Revisions([Region, Region + 1, Region + 2]);

        Assert.Equal(new RegionMapRevision(1, 0), revisions[Region]);
        Assert.Equal(new RegionMapRevision(0, -1), revisions[Region + 1]);
        Assert.Equal(new RegionMapRevision(-1, -1), revisions[Region + 2]);
        Assert.Equal(1, index.TableReads);
    }

    [Fact]
    public void ReadTerrain_DecodesTheTilesAndTrailer()
    {
        var index = new FakeIndex();
        index.Store("m50_50", StoredTerrain);

        var terrain = new RegionMapArchive(index.Cache.Object).ReadTerrain(Region).Value;

        Assert.Equal(MapRenderDecoder.DecodeTerrain(new MemoryStream(StoredTerrain), Region)[0].Underlays, terrain.Planes[0].Underlays);
        Assert.NotNull(terrain.Atmosphere);
        Assert.False(terrain.TrailerTruncated);
    }

    [Fact]
    public void ReadTerrain_ReportsATruncatedTrailerAsTerrainWithDefaultLighting()
    {
        var index = new FakeIndex();
        index.Store("m50_50", [.. new byte[4 * 64 * 64], 0, 1]);

        var terrain = new RegionMapArchive(index.Cache.Object).ReadTerrain(Region).Value;

        Assert.True(terrain.TrailerTruncated);
        Assert.Null(terrain.Atmosphere);
    }

    [Fact]
    public void Reads_ReportAbsentAndUnreadableFilesWithTheirRegionAndKind()
    {
        var index = new FakeIndex();
        index.Store("m50_50", [51]);
        var archive = new RegionMapArchive(index.Cache.Object);

        var unreadable = Assert.Single(archive.ReadTerrain(Region).Errors.OfType<RegionMapFileUnreadableError>());
        var absent = Assert.Single(archive.ReadLandscape(Region).Errors.OfType<RegionMapFileAbsentError>());

        Assert.Equal((Region, RegionMapFile.Terrain), (unreadable.Region, unreadable.Kind));
        Assert.IsType<EndOfStreamException>(unreadable.Cause);
        Assert.Equal((Region, RegionMapFile.Landscape), (absent.Region, absent.Kind));
    }

    [Fact]
    public void ReadLandscape_TreatsAPlacementOutsideTheSourcePlanesAsUnreadable()
    {
        var index = new FakeIndex();
        // Object 0 at location (plane 4, 0, 0): the location step is (4 << 12) + 1 as a two-byte smart.
        index.Store("l50_50", [0x01, 0xC0, 0x01, 0x00, 0x00, 0x00]);

        var error = Assert.Single(new RegionMapArchive(index.Cache.Object).ReadLandscape(Region).Errors.OfType<RegionMapFileUnreadableError>());

        Assert.IsType<InvalidDataException>(error.Cause);
    }

    [Fact]
    public void ReadUnderwaterLandscape_PropagatesCancellation()
    {
        var index = new FakeIndex();
        index.Store("ul50_50", StoredLandscape);

        Assert.Throws<OperationCanceledException>(() => new RegionMapArchive(index.Cache.Object).ReadUnderwaterLandscape(Region, new CancellationToken(true)));
    }

    [Fact]
    public void ApplyTerrain_RepatchesWhenAnotherChangeLandsBeforeTheWrite()
    {
        var index = new FakeIndex();
        index.Store("m50_50", StoredTerrain);
        var archive = new RegionMapArchive(index.Cache.Object);
        index.BeforeReplace = () =>
        {
            // Another tile change is written after this one read the file but before it writes.
            index.BeforeReplace = () => { };
            Assert.True(archive.ApplyTerrain(Region, [new MapTerrainTileChange(0, 30, 40, Underlay: 5)]).IsSuccess);
        };

        Assert.True(archive.ApplyTerrain(Region, [new MapTerrainTileChange(0, 10, 20, Underlay: 3)]).IsSuccess);

        var planes = MapRenderDecoder.DecodeTerrain(new MemoryStream(index.Read("m50_50")), Region);
        Assert.Equal(3, planes[0].Underlays[10 * 64 + 20]);
        Assert.Equal(5, planes[0].Underlays[30 * 64 + 40]);
        Assert.Equal(CompressionType.Gzip, index.CompressionOf("m50_50"));
        index.Cache.Verify(api => api.Write(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IContainer>()), Times.Never);
    }

    [Fact]
    public void ApplyTerrain_ReportsUnreadableTerrainWithoutWriting()
    {
        var index = new FakeIndex();
        index.Store("m50_50", [51]);

        var result = new RegionMapArchive(index.Cache.Object).ApplyTerrain(Region, [new MapTerrainTileChange(0, 0, 0, Underlay: 3)]);

        Assert.True(result.HasError<RegionMapFileUnreadableError>());
        Assert.Equal(0, index.Replacements);
    }

    [Fact]
    public void ApplyTerrain_WritesNothingWhenTheTilesAlreadyHoldTheChange()
    {
        var index = new FakeIndex();
        index.Store("m50_50", StoredTerrain);
        var underlay = MapRenderDecoder.DecodeTerrain(new MemoryStream(StoredTerrain), Region)[0].Underlays[10 * 64 + 20];

        Assert.True(new RegionMapArchive(index.Cache.Object).ApplyTerrain(Region, [new MapTerrainTileChange(0, 10, 20, Underlay: underlay)]).IsSuccess);

        Assert.Equal(0, index.Replacements);
    }

    [Fact]
    public void ApplyTerrain_ReportsAnAbsentTerrain()
    {
        var index = new FakeIndex();

        Assert.True(new RegionMapArchive(index.Cache.Object).ApplyTerrain(Region, []).HasError<RegionMapFileAbsentError>());
    }

    [Fact]
    public void ReplaceLandscape_ChecksAgainAndWritesWhenAnotherSaveLandsBeforeTheWrite()
    {
        var index = new FakeIndex();
        index.Store("l50_50", StoredLandscape, CompressionType.Bzip2);
        var archive = new RegionMapArchive(index.Cache.Object);
        MapRenderPlacement[] mine = [new(1, 10, 0, 1, 1, 0)];
        var reads = 0;
        index.BeforeReplace = () =>
        {
            index.BeforeReplace = () => { };
            Assert.True(archive.ReplaceLandscape(Region, [new MapRenderPlacement(2, 10, 0, 2, 2, 0)]).IsSuccess);
            reads = index.ContainerReads;
        };

        Assert.True(archive.ReplaceLandscape(Region, mine).IsSuccess);

        Assert.Equal(mine, MapRenderDecoder.DecodeLandscape(new MemoryStream(index.Read("l50_50"))));
        Assert.True(index.ContainerReads > reads, "The save must read the newly stored landscape again before writing.");
        Assert.Equal(CompressionType.Bzip2, index.CompressionOf("l50_50"));
        index.Cache.Verify(api => api.Write(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IContainer>()), Times.Never);
    }

    [Fact]
    public void ReplaceLandscape_RefusesALandscapeThatBecameUnreadableBeforeTheWrite()
    {
        var index = new FakeIndex();
        index.Store("l50_50", StoredLandscape);
        index.BeforeReplace = () =>
        {
            index.BeforeReplace = () => { };
            index.Store("l50_50", [0x80]);
        };

        var result = new RegionMapArchive(index.Cache.Object).ReplaceLandscape(Region, []);

        Assert.True(result.HasError<RegionMapFileUnreadableError>());
        Assert.Equal([0x80], index.Read("l50_50"));
    }

    [Fact]
    public void ReplaceLandscape_WritesNothingWhenThePlacementsAreTheStoredOnes()
    {
        var index = new FakeIndex();
        index.Store("l50_50", StoredLandscape);

        var result = new RegionMapArchive(index.Cache.Object).ReplaceLandscape(Region, MapRenderDecoder.DecodeLandscape(new MemoryStream(StoredLandscape)));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, index.Replacements);
    }

    [Fact]
    public void ReplaceLandscape_ReportsAnAbsentLandscape()
    {
        var index = new FakeIndex();

        Assert.True(new RegionMapArchive(index.Cache.Object).ReplaceLandscape(Region, []).HasError<RegionMapFileAbsentError>());
    }

    private static byte[] Fixture(string name) => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "maps", name));

    /// <summary>Index 5 in memory: named files whose version rises with every write, and a compare-and-swap like the cache's.</summary>
    private sealed class FakeIndex
    {
        private readonly Dictionary<string, (int File, byte[] Data, CompressionType Compression, int Version)> _files = new();

        public FakeIndex()
        {
            Cache.Setup(api => api.GetFileId(5, It.IsAny<string>())).Returns((int _, string name) => _files.TryGetValue(name, out var file) ? file.File : -1);
            Cache.Setup(api => api.ReadReferenceTable(5)).Returns(() =>
            {
                TableReads++;
                return Table();
            });
            Cache.Setup(api => api.ReadContainer(5, It.IsAny<int>())).Returns((int _, int file) =>
            {
                ContainerReads++;
                var stored = _files.Values.Single(entry => entry.File == file);
                return new Container(stored.Compression, new MemoryStream(stored.Data));
            });
            Cache.Setup(api => api.TryReplaceContainer(5, It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<IContainer>()))
                .Returns((int _, int file, byte[] expected, IContainer replacement) =>
                {
                    BeforeReplace();
                    var name = _files.Single(entry => entry.Value.File == file).Key;
                    if (!expected.AsSpan().SequenceEqual(_files[name].Data)) return false;
                    Replacements++;
                    Store(name, replacement.Data.ToArray(), replacement.CompressionType);
                    return true;
                });
        }

        public Mock<ICacheAPI> Cache { get; } = new(MockBehavior.Strict);
        public bool Named { get; init; } = true;
        public Action BeforeReplace { get; set; } = () => { };
        public int TableReads { get; private set; }
        public int ContainerReads { get; private set; }
        public int Replacements { get; private set; }

        /// <summary>Stores a file, bumping its version the way a cache write does.</summary>
        public void Store(string name, byte[] data, CompressionType compression = CompressionType.Gzip) =>
            _files[name] = _files.TryGetValue(name, out var stored)
                ? (stored.File, data, compression, stored.Version + 1)
                : (_files.Count + 1, data, compression, 0);

        public int FileOf(string name) => _files[name].File;
        public byte[] Read(string name) => _files[name].Data;
        public CompressionType CompressionOf(string name) => _files[name].Compression;

        private IReferenceTable Table()
        {
            var table = new Mock<IReferenceTable>();
            table.Setup(t => t.Flags).Returns(Named ? ReferenceTableFlags.Identifiers : 0);
            table.Setup(t => t.Entries).Returns(() => _files.ToDictionary(
                entry => entry.Value.File,
                entry => Mock.Of<IReferenceTableEntry>(e => e.Id == ReferenceTable.CalculateId(entry.Key) && e.Version == entry.Value.Version)));
            table.Setup(t => t.GetFileId(It.IsAny<string>())).Returns((string name) => _files.TryGetValue(name, out var file) ? file.File : -1);
            table.Setup(t => t.GetEntry(It.IsAny<int>())).Returns((int file) =>
                _files.Values.Where(entry => entry.File == file).Select(entry => Mock.Of<IReferenceTableEntry>(e => e.Version == entry.Version)).SingleOrDefault());
            return table.Object;
        }
    }
}
