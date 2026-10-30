using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Hagalaz.Cache.Logic.Codecs;

public static class TextureImageDecoder
{
    public sealed record HdrImage(int Width, int Height, float[] Rgba);

    /// <summary>TextureLoader.method649: packed RGB companion low 12 bits scale source RGB.</summary>
    public static HdrImage ToHdr(byte[] data, byte[] companion, int alphaMode)
    {
        using var image = Image.Load<Rgba32>(data);
        ApplyAlpha(image, alphaMode);
        using var hdr = Image.Load<Rgba32>(companion);
        if (hdr.Width != image.Width || hdr.Height != image.Height)
            throw new System.NotSupportedException("HDR companion dimensions do not match the texture.");
        var values = new float[image.Width * image.Height * 4];
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
        {
            var pixel = image[x, y];
            var sample = hdr[x, y];
            var multiplier = 1 + (((sample.G << 8) | sample.B) & 0xfff) * 31f / 4096;
            var offset = (y * image.Width + x) * 4;
            values[offset] = pixel.R / 255f * multiplier;
            values[offset + 1] = pixel.G / 255f * multiplier;
            values[offset + 2] = pixel.B / 255f * multiplier;
            values[offset + 3] = pixel.A / 255f;
        }
        return new HdrImage(image.Width, image.Height, values);
    }

    /// <summary>Converts a cache image to PNG, retaining the material's decoded alpha.</summary>
    public static byte[] ToPng(byte[] data, int alphaMode)
    {
        using var image = Image.Load<Rgba32>(data);
        ApplyAlpha(image, alphaMode);
        using var output = new MemoryStream();
        // Source PNG metadata may specify RGB/palette encoding, which otherwise drops the new alpha.
        image.SaveAsPng(output, new PngEncoder { ColorType = PngColorType.RgbWithAlpha });
        return output.ToArray();
    }

    private static void ApplyAlpha(Image<Rgba32> image, int alphaMode)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    ref var pixel = ref row[x];
                    if (alphaMode == 0) pixel.A = 255;
                    else if (alphaMode == 1) pixel.A = pixel.R == 0 && pixel.G == 0 && pixel.B == 0 ? (byte)0 : (byte)255;
                }
            }
        });
    }
}
