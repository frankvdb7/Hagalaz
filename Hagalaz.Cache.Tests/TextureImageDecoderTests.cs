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
