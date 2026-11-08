using System.Buffers.Binary;
using System.Security.Cryptography;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class MapRenderDecoderTests
{
    [Fact]
    public void LandscapeProjection_MatchesClientBufferTraversalForRealRegion()
    {
        using var input = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "maps", "landscape-12850.dat"));
        var placements = MapRenderDecoder.DecodeLandscape(input);
        Assert.Equal(5934, placements.Length);
        using var semantic = new MemoryStream();
        foreach (var placement in placements)
        foreach (var value in new[] { placement.Id, placement.Shape, placement.Rotation, placement.X, placement.Y, placement.Plane })
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(bytes, value);
            semantic.Write(bytes);
        }
        Assert.Equal("bee90dfdcbf5e934e4d0c9faac97959490dbb2da4a44882c5891892513374077", Convert.ToHexStringLower(SHA256.HashData(semantic.ToArray())));
    }

    [Fact]
    public void TerrainProjection_MatchesIndependentClientForEveryTileOfRealRegion()
    {
        using var input = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "maps", "terrain-12850.dat"));
        var terrain = MapRenderDecoder.DecodeTerrain(input, 12850);
        var atmosphere = MapRenderDecoder.DecodeAtmosphere(input);
        Assert.Equal(new Hagalaz.Cache.Types.MapAtmosphere(0xddcebb, 334 / 256f, 2f, 92 / 256f, -4, -16, 1, 0x8da4c2, 600), atmosphere);
        using var semantic = new MemoryStream();
        foreach (var p in terrain)
        for (var tile = 0; tile < 4096; tile++)
        foreach (var value in new[] { p.Heights[tile], p.Flags[tile], p.Underlays[tile], p.Overlays[tile], p.OverlayShapes[tile], p.OverlayRotations[tile] })
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(bytes, value);
            semantic.Write(bytes);
        }
        // Captured by calling GameClient Class274.readLandscapeData, not by this decoder.
        Assert.Equal("bafd725f7085f1f8ffe9349d523c9704b007dd8de1a7900c536c6623d51baceb", Convert.ToHexStringLower(SHA256.HashData(semantic.ToArray())));
    }

    [Theory]
    [InlineData(new byte[] { 0, 0 }, 0xc8c0a8, 0)]
    [InlineData(new byte[] { 0, 0x60, 0xff, 0x12, 0x34, 0x56, 0x02, 0x58 }, 0x123456, 600)]
    public void AtmosphereProjection_ReadsSkyColourAndRawFogValue(byte[] record, int skyRgb, int fogValue)
    {
        var atmosphere = MapRenderDecoder.DecodeAtmosphere(new MemoryStream(record));
        Assert.NotNull(atmosphere);
        Assert.Equal(skyRgb, atmosphere.SkyRgb);
        Assert.Equal(fogValue, atmosphere.FogValue);
        Assert.Equal(0xffffff, atmosphere.LightRgb);
    }

    [Fact]
    public void TerrainProjection_PreservesFullTileSemanticsAndPlaneSpacing()
    {
        using var stream = new MemoryStream(new byte[] { 49, 200, 51, 255, 1, 5 }.Concat(new byte[16383]).ToArray());
        var planes = MapRenderDecoder.DecodeTerrain(stream, 258);
        Assert.Equal(4, planes.Length);
        Assert.Equal(-160, planes[0].Heights[0]);
        Assert.Equal(-1120, planes[1].Heights[0]);
        Assert.Equal(-3040, planes[3].Heights[0]);
        Assert.Equal(2, planes[0].Flags[0]);
        Assert.Equal(174, planes[0].Underlays[0]);
        Assert.Equal(200, planes[0].Overlays[0]);
        Assert.Equal(11, planes[0].OverlayShapes[0]);
        Assert.Equal(3, planes[0].OverlayRotations[0]);
    }

    [Fact]
    public void LandscapeProjection_PreservesSourcePlaneAndPlacement()
    {
        using var stream = new MemoryStream();
        stream.WriteHugeSmart(42);
        stream.WriteSmart((1 << 12) + (3 << 6) + 4 + 1);
        stream.WriteByte(43);
        stream.WriteSmart(0);
        stream.WriteHugeSmart(0);
        stream.Position = 0;
        var objects = MapRenderDecoder.DecodeLandscape(stream);
        Assert.Equal(new Hagalaz.Cache.Types.MapRenderPlacement(41, 10, 3, 3, 4, 1), Assert.Single(objects));
    }

    [Fact]
    public void FloorProjection_PreservesColorsTexturesAndScale()
    {
        using var overlay = new MemoryStream([1, 0x12, 0x34, 0x56, 3, 255, 255, 7, 0x65, 0x43, 0x21, 9, 0, 64, 5, 8, 10, 11, 1, 12, 13, 0, 0, 0, 14, 1, 16, 2, 20, 0, 3, 21, 4, 22, 0, 5, 0]);
        var result = MapRenderDecoder.DecodeFloor(overlay, 3, true);
        Assert.Equal(0x123456, result.Rgb);
        Assert.Equal(0x654321, result.SecondaryRgb);
        Assert.Equal(-1, result.TextureId);
        Assert.Equal(256, result.TextureScale);
        using var underlay = new MemoryStream([1, 0, 255, 0, 2, 0, 7, 3, 0, 32, 4, 5, 0]);
        var ground = MapRenderDecoder.DecodeFloor(underlay, 8, false);
        Assert.Equal(0x00ff00, ground.Rgb);
        Assert.Equal(7, ground.TextureId);
        Assert.Equal(128, ground.TextureScale);
    }

    [Fact]
    public void RenderDecoders_RejectTruncationAndHonorCancellation()
    {
        Assert.Throws<EndOfStreamException>(() => MapRenderDecoder.DecodeTerrain(new MemoryStream([1]), 0));
        Assert.Throws<EndOfStreamException>(() => MapRenderDecoder.DecodeLandscape(new MemoryStream([1, 128])));
        Assert.Throws<NotSupportedException>(() => MapRenderDecoder.DecodeFloor(new MemoryStream([99]), 1, true));
        Assert.Throws<OperationCanceledException>(() => MapRenderDecoder.DecodeTerrain(new MemoryStream(new byte[16384]), 0, new CancellationToken(true)));
        Assert.Throws<ArgumentOutOfRangeException>(() => MapRenderDecoder.DecodeTerrain(new MemoryStream(), 65536));
    }
}
