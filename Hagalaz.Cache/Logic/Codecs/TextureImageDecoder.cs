using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Hagalaz.Cache.Logic.Codecs;

public static class TextureImageDecoder
{
    /// <summary>Converts a cache image to PNG, retaining the material's decoded alpha.</summary>
    public static byte[] ToPng(byte[] data, int alphaMode)
    {
        using var image = Image.Load<Rgba32>(data);
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
        using var output = new MemoryStream();
        // Source PNG metadata may specify RGB/palette encoding, which otherwise drops the new alpha.
        image.SaveAsPng(output, new PngEncoder { ColorType = PngColorType.RgbWithAlpha });
        return output.ToArray();
    }
}
