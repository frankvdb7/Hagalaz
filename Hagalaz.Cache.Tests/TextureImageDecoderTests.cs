using System.IO;
using Hagalaz.Cache.Logic.Codecs;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Hagalaz.Cache.Tests;

public class TextureImageDecoderTests
{
    [Fact]
    public void ToHdr_UsesPackedLowTwelveBitsAndPreservesAlphaAndRowOrder()
    {
        using var source = new Image<Rgba32>(1, 2);
        source[0, 0] = new Rgba32(255, 128, 64, 96);
        source[0, 1] = new Rgba32(0, 0, 0, 255);
        using var companion = new Image<Rgba32>(1, 2);
        companion[0, 0] = new Rgba32(233, 248, 0, 255); // 0x800, not red or upper green bits.
        companion[0, 1] = new Rgba32(255, 255, 255, 255);
        using var sourceBytes = new MemoryStream(); source.SaveAsPng(sourceBytes);
        using var hdrBytes = new MemoryStream(); companion.SaveAsPng(hdrBytes);
        var result = TextureImageDecoder.ToHdr(sourceBytes.ToArray(), hdrBytes.ToArray(), 2);
        Assert.Equal(1, result.Width); Assert.Equal(2, result.Height);
        Assert.Equal(16.5f, result.Rgba[0]);
        Assert.Equal(128 / 255f * 16.5f, result.Rgba[1]);
        Assert.Equal(64 / 255f * 16.5f, result.Rgba[2]);
        Assert.Equal(96 / 255f, result.Rgba[3]);
        Assert.Equal(new float[] {0, 0, 0, 1}, result.Rgba[4..]);
        Assert.Equal(0, TextureImageDecoder.ToHdr(sourceBytes.ToArray(), hdrBytes.ToArray(), 1).Rgba[7]);
    }

    [Fact]
    public void ToHdr_ExpandsGrayscaleCompanionBeforeTakingPackedLowBits()
    {
        using var source = new Image<Rgba32>(1, 1, new Rgba32(255, 255, 255, 10));
        using var companion = new Image<Rgba32>(1, 1, new Rgba32(128, 128, 128));
        using var sourceBytes = new MemoryStream(); source.SaveAsPng(sourceBytes);
        using var hdrBytes = new MemoryStream();
        companion.SaveAsPng(hdrBytes, new PngEncoder {ColorType = PngColorType.Grayscale});
        var result = TextureImageDecoder.ToHdr(sourceBytes.ToArray(), hdrBytes.ToArray(), 0);
        Assert.Equal(new float[] {1.96875f, 1.96875f, 1.96875f, 1}, result.Rgba);
    }

    [Fact]
    public void ToHdr_RejectsMismatchedCompanionDimensions()
    {
        using var source = new Image<Rgba32>(1, 1);
        using var companion = new Image<Rgba32>(2, 1);
        using var sourceBytes = new MemoryStream(); source.SaveAsPng(sourceBytes);
        using var hdrBytes = new MemoryStream(); companion.SaveAsPng(hdrBytes);
        Assert.Throws<NotSupportedException>(() => TextureImageDecoder.ToHdr(sourceBytes.ToArray(), hdrBytes.ToArray(), 0));
    }

    [Fact]
    public void ToPng_CutoutAlphaSurvivesRgbSourceMetadata()
    {
        using var source = new Image<Rgba32>(2, 1);
        source[0, 0] = new Rgba32(0, 0, 0, 255);
        source[1, 0] = new Rgba32(80, 92, 42, 255);
        using var input = new MemoryStream();
        source.SaveAsPng(input, new PngEncoder { ColorType = PngColorType.Rgb });

        using var result = Image.Load<Rgba32>(TextureImageDecoder.ToPng(input.ToArray(), 1));

        Assert.Equal(new Rgba32(0, 0, 0, 0), result[0, 0]);
        Assert.Equal(new Rgba32(80, 92, 42, 255), result[1, 0]);
    }

    [Theory]
    [InlineData(0, 255)]
    [InlineData(2, 96)]
    public void ToPng_RespectsOpaqueAndBlendedModes(int mode, byte expectedAlpha)
    {
        using var source = new Image<Rgba32>(1, 1);
        source[0, 0] = new Rgba32(0, 0, 0, 96);
        using var input = new MemoryStream();
        source.SaveAsPng(input, new PngEncoder { ColorType = PngColorType.RgbWithAlpha });

        using var result = Image.Load<Rgba32>(TextureImageDecoder.ToPng(input.ToArray(), mode));

        Assert.Equal(expectedAlpha, result[0, 0].A);
    }
}
