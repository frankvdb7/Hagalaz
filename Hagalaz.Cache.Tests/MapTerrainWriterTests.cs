using System;
using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Providers;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class MapTerrainWriterTests
{
    private const int Region = 12850;
    private const int TerrainFile = 3;

    [Fact]
    public void Apply_PatchesTheFileAsStoredWhenTheCacheRunsTheChange()
    {
        var stored = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "maps", "terrain-12850.dat"));
        var cache = Cache(() => stored, updated => stored = updated);
        var writer = new MapTerrainWriter(cache.Object);

        // Each request patches what the cache hands it under its write lock, so neither change is lost.
        Assert.Equal(MapArchiveUpdateResult.Updated, writer.Apply(Region, [new MapTerrainTileChange(0, 10, 20, Underlay: 3)]));
        Assert.Equal(MapArchiveUpdateResult.Updated, writer.Apply(Region, [new MapTerrainTileChange(0, 30, 40, Underlay: 5)]));

        var planes = MapRenderDecoder.DecodeTerrain(new MemoryStream(stored), Region);
        Assert.Equal(3, planes[0].Underlays[10 * 64 + 20]);
        Assert.Equal(5, planes[0].Underlays[30 * 64 + 40]);
        cache.Verify(api => api.Write(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IContainer>()), Times.Never);
    }

    [Fact]
    public void Apply_ReportsUnreadableTerrainWithoutWriting()
    {
        var written = false;
        var cache = Cache(() => [51], _ => written = true);

        Assert.Equal(MapArchiveUpdateResult.Unreadable, new MapTerrainWriter(cache.Object).Apply(Region, [new MapTerrainTileChange(0, 0, 0, Underlay: 3)]));
        Assert.False(written);
    }

    private static Mock<ICacheAPI> Cache(Func<byte[]> read, Action<byte[]> write)
    {
        var cache = new Mock<ICacheAPI>(MockBehavior.Strict);
        cache.Setup(api => api.GetFileId(5, $"m{Region >> 8}_{Region & 255}")).Returns(TerrainFile);
        cache.Setup(api => api.UpdateContainer(5, TerrainFile, It.IsAny<Func<IContainer, IContainer?>>()))
            .Returns((int _, int _, Func<IContainer, IContainer?> change) =>
            {
                using var current = new Container(CompressionType.Gzip, new MemoryStream(read()));
                using var updated = change(current);
                if (updated is null) return false;
                Assert.Equal(CompressionType.Gzip, updated.CompressionType);
                write(updated.Data.ToArray());
                return true;
            });
        return cache;
    }
}
