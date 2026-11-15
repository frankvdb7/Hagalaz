using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Logic.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Hagalaz.Services.Cache.Features.Types;

/// <summary>HTTP validators and PNG encoding for rendered item icons and model images.</summary>
internal static class ItemIcons
{
    internal readonly record struct Options(int Width, int Height, int Outline, int? Shadow, int Quantity, int? QuantityMode);

    // Bump when the rendered output changes for the same cache contents.
    private const string RendererVersion = "v1";

    // Raw store writes bypass reference-table versions, so each cache instance carries a random generation that they replace.
    // Being random, it also invalidates validators issued by earlier processes.
    private static readonly ConditionalWeakTable<ICacheAPI, string> RawGenerations = new();

    /// <summary>Invalidates validators after writes that keep reference-table versions, such as raw store writes.</summary>
    internal static void InvalidateValidators(ICacheAPI cache) => RawGenerations.AddOrUpdate(cache, Guid.NewGuid().ToString("N"));

    /// <summary>
    /// A validator computed without rendering. It changes whenever a source index's reference-table version changes,
    /// which every metadata-tracked cache write does, so a matching request can skip the render.
    /// </summary>
    public static string Validator(int id, object options, ICacheAPI cache)
    {
        var versions = ItemIconComposer.SourceIndexes.Select(index =>
        {
            // Absent indexes render as missing inputs, as TextureSource and Font() already handle.
            try { return cache.ReadReferenceTable(index).Version; }
            catch (FileNotFoundException) { return -1; }
        });
        var generation = RawGenerations.GetValue(cache, _ => Guid.NewGuid().ToString("N"));
        var key = $"{RendererVersion}|{generation}|{id}|{options}|{string.Join(',', versions)}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }

    internal static byte[] EncodePng(int[] pixels, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var argb = pixels[y * width + x];
                    row[x] = new Rgba32((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, (byte)(argb >>> 24));
                }
            }
        });
        using var output = new MemoryStream();
        image.SaveAsPng(output, new PngEncoder { ColorType = PngColorType.RgbWithAlpha });
        return output.ToArray();
    }
}
