using System.Text.Json;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types.Factories;
using Hagalaz.Cache.Types.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests;

/// <summary>
/// The web editor's walkability rule is checked against the same two fixtures, so the editor and the game
/// server's terrain blocking cannot drift apart unnoticed.
/// </summary>
public sealed class MapTerrainBlockingAgreementTests
{
    private const int Region = 12850;

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", "maps", name);

    [Fact]
    public void RenderDecoder_ReproducesThePinnedFlags()
    {
        var expected = JsonSerializer.Deserialize<int[][]>(File.ReadAllText(Fixture("flags-12850.json")))!;

        using var terrain = File.OpenRead(Fixture("terrain-12850.dat"));
        var planes = MapRenderDecoder.DecodeTerrain(terrain, Region);

        Assert.Equal(expected, planes.Select(plane => plane.Flags).ToArray());
    }

    [Fact]
    public void GameServerDecoding_ReproducesThePinnedBlockedTiles()
    {
        var expected = JsonSerializer.Deserialize<int[]>(File.ReadAllText(Fixture("blocked-12850.json")))!;
        var cache = new Mock<ICacheAPI>();
        cache.Setup(api => api.GetFileId(5, "m50_50")).Returns(1);
        cache.Setup(api => api.GetFileId(5, "l50_50")).Returns(-1);
        cache.Setup(api => api.ReadContainer(5, 1)).Returns(() => new Container(new MemoryStream(File.ReadAllBytes(Fixture("terrain-12850.dat")))));
        var provider = new MapProvider(cache.Object, Mock.Of<IMapCodec>(), new MapTypeFactory(), NullLogger<MapProvider>.Instance);

        var blocked = provider.DecodeRegion(Region, [], CancellationToken.None).BlockedTiles
            .Select(tile => (tile.Z << 12) | (tile.LocalX << 6) | tile.LocalY).Order().ToArray();

        Assert.Equal(expected, blocked);
        // 32 blocked ground tiles under this region's plane-1 bridge are dropped, so the bridge rule is exercised.
        Assert.Equal(2454, blocked.Length);
    }
}
