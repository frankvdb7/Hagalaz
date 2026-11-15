using System;
using System.Globalization;
using System.Text;

namespace Hagalaz.Cache.Logic.Rendering;

/// <summary>
/// A software-client bitmap font: FontDefinition metrics (font index) plus one 1-bit glyph mask per character (sprite index).
/// Draws like Class263.renderText with Class263_Sub3, including colour tags and the one-pixel text shadow.
/// </summary>
public sealed class ClientFont
{
    private static readonly Encoding Windows1252 = CodePagesEncoding();
    private readonly int[] _advances;
    private readonly Glyph[] _glyphs;

    /// <summary>A glyph mask covering the whole sprite-set canvas; set pixels are drawn in the text colour.</summary>
    public sealed record Glyph(int Width, int Height, bool[] Mask);

    public ClientFont(byte[] metrics, Glyph[] glyphs)
    {
        // FontDefinition: version 0, kerning flag, 256 advances, then (without kerning) the ascent and three more bytes.
        if (metrics.Length < 258 || metrics[0] != 0) throw new NotSupportedException("Unsupported font metrics version.");
        if (metrics[1] != 0) throw new NotSupportedException("Kerned font metrics are not supported.");
        if (metrics.Length < 263) throw new NotSupportedException("Font metrics are truncated.");
        if (glyphs.Length < 256) throw new NotSupportedException("The font sprite set must contain 256 glyphs.");
        _advances = new int[256];
        for (var c = 0; c < 256; c++) _advances[c] = metrics[2 + c];
        Ascent = metrics[258];
        _glyphs = glyphs;
    }

    /// <summary>FontDefinition.anInt6030: the distance from the drawing baseline to the glyph canvas top.</summary>
    public int Ascent { get; }

    /// <summary>
    /// Class263.method2594: draws text with its baseline at <paramref name="baseline"/>, shadow first at (+1, +1).
    /// <paramref name="scale"/> enlarges every glyph pixel for icons rendered above the client size, and
    /// (<paramref name="originX"/>, <paramref name="originY"/>) is where the client 36×32 frame starts in the output.
    /// </summary>
    public void Draw(int[] pixels, int width, int height, string text, int x, int baseline, int color, int shadow, int scale = 1, int originX = 0, int originY = 0)
    {
        var top = baseline - Ascent;
        var current = color;
        var tag = -1;
        for (var index = 0; index < text.Length; index++)
        {
            var ch = text[index];
            if (ch == '<') { tag = index; continue; }
            if (tag != -1)
            {
                if (ch != '>') continue;
                var name = text.Substring(tag + 1, index - tag - 1);
                tag = -1;
                if (name.StartsWith("col=", StringComparison.Ordinal) && int.TryParse(name.AsSpan(4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var rgb))
                    current = (color & unchecked((int)0xFF000000)) | rgb;
                else if (name == "/col") current = color;
                continue;
            }
            var code = Windows1252.GetBytes(ch.ToString())[0];
            if (code != ' ')
            {
                if ((shadow & unchecked((int)0xFF000000)) != 0) DrawGlyph(pixels, width, height, _glyphs[code], x + 1, top + 1, shadow, scale, originX, originY);
                DrawGlyph(pixels, width, height, _glyphs[code], x, top, current, scale, originX, originY);
            }
            x += _advances[code];
        }
    }

    private static void DrawGlyph(int[] pixels, int width, int height, Glyph glyph, int x, int y, int color, int scale, int originX, int originY)
    {
        for (var gy = 0; gy < glyph.Height; gy++)
        for (var gx = 0; gx < glyph.Width; gx++)
        {
            if (!glyph.Mask[gy * glyph.Width + gx]) continue;
            for (var sy = 0; sy < scale; sy++)
            for (var sx = 0; sx < scale; sx++)
            {
                int px = originX + (x + gx) * scale + sx, py = originY + (y + gy) * scale + sy;
                if (px >= 0 && py >= 0 && px < width && py < height) pixels[py * width + px] = color;
            }
        }
    }

    private static Encoding CodePagesEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1252, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);
    }
}
