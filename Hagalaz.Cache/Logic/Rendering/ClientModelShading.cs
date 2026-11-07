using System;

namespace Hagalaz.Cache.Logic.Rendering;

/// <summary>
/// Software-client model lighting and palette (Model_Sub3.method4549, Class356.method4187 and the fixed-gamma palettes).
/// The browser model viewer ports the same client routines; this is the server-side counterpart.
/// </summary>
public static class ClientModelShading
{
    /// <summary>Client light parameters, in the units passed to GraphicsToolkit.IA and GraphicsToolkit.m.</summary>
    public readonly record struct Light(float Intensity, float PositiveResponse, float NegativeResponse, int DirectionX, int DirectionY, int DirectionZ);

    /// <summary>Per-corner shades for indices, using smooth vertex normals unless a face is flat (shading type 1).</summary>
    public static int[] LightModel(ReadOnlySpan<int> positions, ReadOnlySpan<int> indices, ReadOnlySpan<int> faceShadingTypes, int contrast, Light light)
    {
        var vertexCount = positions.Length / 3;
        var sums = new int[vertexCount * 4];
        var faceCount = indices.Length / 3;
        var normals = new int[faceCount * 3];
        for (var face = 0; face < faceCount; face++)
        {
            int a = indices[face * 3] * 3, b = indices[face * 3 + 1] * 3, c = indices[face * 3 + 2] * 3;
            int abX = positions[b] - positions[a], abY = positions[b + 1] - positions[a + 1], abZ = positions[b + 2] - positions[a + 2];
            int acX = positions[c] - positions[a], acY = positions[c + 1] - positions[a + 1], acZ = positions[c + 2] - positions[a + 2];
            var x = abY * acZ - acY * abZ;
            var y = abZ * acX - acZ * abX;
            var z = abX * acY - acX * abY;
            while (Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z))) > 8192) { x >>= 1; y >>= 1; z >>= 1; }
            var length = Math.Max(1, (int)Math.Sqrt((double)x * x + (double)y * y + (double)z * z));
            normals[face * 3] = x * 256 / length;
            normals[face * 3 + 1] = y * 256 / length;
            normals[face * 3 + 2] = z * 256 / length;
            if (Type(faceShadingTypes, face) != 0) continue;
            foreach (var vertex in new[] { a / 3, b / 3, c / 3 })
            {
                for (var axis = 0; axis < 3; axis++) sums[vertex * 4 + axis] += normals[face * 3 + axis];
                sums[vertex * 4 + 3]++;
            }
        }

        var vector = Math.Sqrt((double)light.DirectionX * light.DirectionX + (double)light.DirectionY * light.DirectionY + (double)light.DirectionZ * light.DirectionZ);
        int lx = (int)(light.DirectionX * 65535 / vector), ly = (int)(light.DirectionY * 65535 / vector), lz = (int)(light.DirectionZ * 65535 / vector);
        // Single-precision light values are widened before scaling, as Math.fround(value) * 65535 in the client port.
        var baseLight = (int)((double)light.Intensity * 65535) >> 8;
        int positive = (int)((double)light.PositiveResponse * 65535), negative = (int)((double)light.NegativeResponse * 65535);
        int positiveResponse = contrast == 0 ? 0 : positive * 768 / contrast, negativeResponse = contrast == 0 ? 0 : negative * 768 / contrast;

        var shades = new int[indices.Length];
        for (var corner = 0; corner < indices.Length; corner++)
        {
            var face = corner / 3;
            int nx, ny, nz, count;
            if (Type(faceShadingTypes, face) == 1) { nx = normals[face * 3]; ny = normals[face * 3 + 1]; nz = normals[face * 3 + 2]; count = 1; }
            else
            {
                var vertex = indices[corner] * 4;
                nx = sums[vertex]; ny = sums[vertex + 1]; nz = sums[vertex + 2]; count = Math.Max(1, sums[vertex + 3]);
            }
            var dot = unchecked(lx * nx + ly * ny + lz * nz) / count >> 16;
            // The client threshold is 256, not the sign of the dot product.
            shades[corner] = (baseLight >> 1) + (unchecked((dot > 256 ? positiveResponse : negativeResponse) * dot) >> 17);
        }
        return shades;
    }

    public static int ClampLight(int value) => Math.Clamp(value, 2, 126);

    /// <summary>Lit sRGB colour (0xRRGGBB) for a packed HSL face colour.</summary>
    public static int LitFaceColor(int packed, int intensity, bool textured, int ambient)
    {
        if (textured) return PaletteColor((packed & 0xff80) | ClampLight((packed & 127) * intensity >> 7), hsv: false);
        var prepared = (packed & ~127) | ((packed & 127) * ambient >> 7);
        var hsv = SoftwareHsv(prepared);
        return PaletteColor((hsv & 0xff80) | ClampLight((hsv & 127) * intensity >> 7), hsv: true);
    }

    private static int Type(ReadOnlySpan<int> types, int face) => face < types.Length ? types[face] : 0;

    private static int SoftwareHsv(int packed)
    {
        var hue = packed >> 10 & 63;
        var saturation = packed >> 3 & 112;
        var lightness = packed & 127;
        saturation = lightness <= 64 ? saturation * lightness >> 7 : saturation * (127 - lightness) >> 7;
        var value = saturation + lightness;
        var hsvSaturation = value != 0 ? (saturation << 8) / value : saturation << 1;
        return (hue << 10 | (hsvSaturation >> 4) << 7 | value) & 65535;
    }

    private static int PaletteColor(int packed, bool hsv)
    {
        var hue = (packed >> 10 & 63) / 64.0 + 1 / 128.0;
        var saturation = (packed >> 7 & 7) / 8.0 + 1 / 16.0;
        var value = (packed & 127) / 128.0;
        double r, g, b;
        if (hsv)
        {
            var sector = hue * 6;
            var index = (int)sector;
            var fraction = sector - index;
            double p = value * (1 - saturation), q = value * (1 - saturation * fraction), t = value * (1 - (1 - fraction) * saturation);
            (r, g, b) = (index % 6) switch
            {
                0 => (value, t, p),
                1 => (q, value, p),
                2 => (p, value, t),
                3 => (p, q, value),
                4 => (t, p, value),
                _ => (value, p, q),
            };
        }
        else
        {
            var q = value < 0.5 ? value * (1 + saturation) : value + saturation - value * saturation;
            var p = 2 * value - q;
            r = Hue(hue + 1 / 3.0); g = Hue(hue); b = Hue(hue - 1 / 3.0);

            double Hue(double channel)
            {
                var t = (channel + 1) % 1;
                return t < 1 / 6.0 ? p + (q - p) * 6 * t : t < 0.5 ? q : t < 2 / 3.0 ? p + (q - p) * (2 / 3.0 - t) * 6 : p;
            }
        }
        // The client jitters gamma around 0.7; rendering uses exactly 0.7.
        return Gamma(r) << 16 | Gamma(g) << 8 | Gamma(b);

        static int Gamma(double channel) => (int)(Math.Pow(channel, 0.7) * 256) & 255;
    }
}
