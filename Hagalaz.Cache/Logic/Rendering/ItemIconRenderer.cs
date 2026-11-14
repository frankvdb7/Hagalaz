using System;
using System.Collections.Generic;

namespace Hagalaz.Cache.Logic.Rendering;

/// <summary>
/// Renders inventory item icons the way the client's ItemDefinition.method6279 does, into ARGB pixel buffers.
/// A pixel value of 0 is transparent, as in the client.
/// </summary>
public static class ItemIconRenderer
{
    public const int ClientWidth = 36;
    public const int ClientHeight = 32;
    public const int DarkOutline = unchecked((int)0xFF000002);
    public const int LightOutline = unchecked((int)0xFFFFFFFF);
    public const int InventoryShadow = 0x302020;

    // ItemDefinition.method6279: IA(0.95..1.05), m(0xFFFFFF, 0.95..1.05, 0.95..1.05, -50, -10, -50). The random range is fixed at its centre.
    private static readonly ClientModelShading.Light IconLight = new(1f, 1f, 1f, -50, -10, -50);
    // Cached models face -Z, so a half turn plus an eighth shows the front three-quarters from slightly above.
    private static readonly Camera PreviewCamera = new(0, 128, 1024 + 256, 0, 0, 0);
    private const float PreviewFill = 0.92f;
    private static readonly int[] Sine = new int[16384];
    private static readonly int[] Cosine = new int[16384];

    static ItemIconRenderer()
    {
        // Class257 tables.
        const double step = 3.834951969714103E-4;
        for (var i = 0; i < 16384; i++)
        {
            Sine[i] = (int)(16384.0 * Math.Sin(i * step));
            Cosine[i] = (int)(16384.0 * Math.Cos(i * step));
        }
    }

    /// <summary>Assembled model geometry in client units (Y down), with scale already applied.</summary>
    public sealed record Model(int[] Positions, int[] Indices, int[] FaceColors, int[] FaceAlpha, int[]? FaceShadingTypes, int[]? FaceTextureIds, float[]? FaceTextureUvs, int Ambient, int Contrast);

    /// <summary>Item 2D icon settings, in definition units.</summary>
    public readonly record struct Camera(int Zoom, int Rotation1, int Rotation2, int Zan2d, int Offset1, int Offset2);
    public readonly record struct WidgetCamera(int Zoom, int Pitch, int Yaw, int Roll, int X, int Y, int Z, int Flags, int BaseWidth, int BaseHeight);

    /// <summary>A decoded texture in ARGB with its material colour settings.</summary>
    public sealed record Texture(int Width, int Height, int[] Argb, int ColorBlend, int Brightness);

    /// <summary>Zoom variants used by the client: the note's inner item uses ×1.5, outline level 2 uses ×1.04.</summary>
    public enum ZoomMode { Normal, Outlined, NoteContent }

    /// <summary>Draws the model only, before outline and shadow passes. Untouched pixels are 0.</summary>
    public static int[] RenderModel(Model model, Camera camera, IReadOnlyDictionary<int, Texture> textures, ZoomMode zoomMode, int width, int height)
    {
        if (model.Positions.Length == 0) return new int[width * height];
        return Rasterize(model, Project(model, camera, zoomMode, width, height), textures, width, height);
    }

    public static int[] RenderWidget(Model model, WidgetCamera camera, IReadOnlyDictionary<int, Texture> textures, int width, int height) =>
        (camera.Flags & 3) == 0 || camera.Zoom <= 0
            ? RenderFitted(model, textures, width, height)
            : Rasterize(model, ProjectWidget(model, camera, width, height), textures, width, height);

    internal static float[] ProjectWidget(Model model, WidgetCamera camera, int width, int height)
    {
        var modern = (camera.Flags & 3) == 2;
        var orthographic = (camera.Flags & 4) != 0;
        var zoom = modern ? camera.Zoom : camera.Zoom << 2;
        var pitch = modern ? camera.Pitch : camera.Pitch << 3;
        var (st, ct) = MathF.SinCos(Radians(pitch));
        var (sy, cy) = MathF.SinCos(Radians(modern ? camera.Yaw : camera.Yaw << 3));
        var (sr, cr) = MathF.SinCos(Radians(modern ? camera.Roll : -(camera.Roll << 3)));
        var focalX = camera.BaseWidth > 0 ? (width << 9) / camera.BaseWidth : 512;
        var focalY = camera.BaseHeight > 0 ? (height << 9) / camera.BaseHeight : 512;
        var centerX = width / 2 + (modern ? 0 : focalX * camera.X >> 9);
        var centerY = height / 2 + (modern ? 0 : focalY * camera.Y >> 9);
        var screen = new float[model.Positions.Length];
        for (var i = 0; i < screen.Length; i += 3)
        {
            float x = model.Positions[i], y = model.Positions[i + 1], z = model.Positions[i + 2];
            if (modern)
            {
                (y, z) = (y * ct - z * st, y * st + z * ct);
                (x, z) = (x * cy + z * sy, -x * sy + z * cy);
                (x, y) = (x * cr - y * sr + camera.X, x * sr + y * cr + camera.Y);
                z += camera.Z;
            }
            else
            {
                (x, y) = (x * cr - y * sr, x * sr + y * cr);
                (x, z) = (x * cy + z * sy, -x * sy + z * cy);
                y += (int)((long)zoom * Sine[pitch & 0x3fff] >> 14);
                z += (int)((long)zoom * Cosine[pitch & 0x3fff] >> 14);
                (y, z) = (y * ct - z * st, y * st + z * ct);
            }
            var distance = orthographic ? zoom : Math.Max(50, z);
            screen[i] = centerX + focalX * x / distance;
            screen[i + 1] = centerY + focalY * y / distance;
            screen[i + 2] = z;
        }
        return screen;
    }

    /// <summary>Draws a model with no authored camera, centred from a fixed three-quarter view and filling most of the image.</summary>
    public static int[] RenderFitted(Model model, IReadOnlyDictionary<int, Texture> textures, int width, int height)
    {
        // Hidden faces and unused vertices must not shrink the framing.
        var drawn = DrawnVertices(model);
        if (drawn.Length == 0) return new int[width * height];
        var centred = model with { Positions = CentredPositions(model.Positions, drawn) };
        var radius = 0d;
        foreach (var vertex in drawn)
            radius = Math.Max(radius, Math.Sqrt(Math.Pow(centred.Positions[vertex * 3], 2) + Math.Pow(centred.Positions[vertex * 3 + 1], 2) + Math.Pow(centred.Positions[vertex * 3 + 2], 2)));
        // Zoom is a quarter of the camera distance, so half the radius keeps every vertex in front of the camera.
        var minimumZoom = Math.Max(1, (int)Math.Ceiling(radius / 2));
        var zoom = Math.Max(minimumZoom, (int)(radius * 8));
        var screen = Project(centred, PreviewCamera with { Zoom = zoom }, ZoomMode.Normal, width, height);
        for (var pass = 0; pass < 6; pass++)
        {
            var bounds = ScreenBounds(screen, drawn);
            var fill = Math.Max((bounds.MaxX - bounds.MinX) / (width * PreviewFill), (bounds.MaxY - bounds.MinY) / (height * PreviewFill));
            if (fill == 0 || Math.Abs(fill - 1) < 0.01f) break;
            zoom = Math.Max(minimumZoom, (int)Math.Round(zoom * fill));
            screen = Project(centred, PreviewCamera with { Zoom = zoom }, ZoomMode.Normal, width, height);
        }
        var final = ScreenBounds(screen, drawn);
        var shiftX = width / 2f - (final.MinX + final.MaxX) / 2;
        var shiftY = height / 2f - (final.MinY + final.MaxY) / 2;
        for (var offset = 0; offset < screen.Length; offset += 3)
        {
            screen[offset] += shiftX;
            screen[offset + 1] += shiftY;
        }
        return Rasterize(centred, screen, textures, width, height);
    }

    private static int[] DrawnVertices(Model model)
    {
        var drawn = new HashSet<int>();
        for (var face = 0; face < model.Indices.Length / 3; face++)
        {
            if (face < model.FaceAlpha.Length && model.FaceAlpha[face] == 255) continue;
            if (model.FaceShadingTypes is { } types && face < types.Length && types[face] == 2) continue;
            for (var corner = 0; corner < 3; corner++) drawn.Add(model.Indices[face * 3 + corner]);
        }
        return [.. drawn];
    }

    private static int[] CentredPositions(int[] positions, int[] drawn)
    {
        var centred = (int[])positions.Clone();
        for (var axis = 0; axis < 3; axis++)
        {
            int min = int.MaxValue, max = int.MinValue;
            foreach (var vertex in drawn)
            {
                min = Math.Min(min, positions[vertex * 3 + axis]);
                max = Math.Max(max, positions[vertex * 3 + axis]);
            }
            var middle = (int)(((long)min + max) / 2);
            for (var offset = axis; offset < positions.Length; offset += 3) centred[offset] -= middle;
        }
        return centred;
    }

    private static (float MinX, float MinY, float MaxX, float MaxY) ScreenBounds(float[] screen, int[] drawn)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var vertex in drawn)
        {
            minX = Math.Min(minX, screen[vertex * 3]);
            maxX = Math.Max(maxX, screen[vertex * 3]);
            minY = Math.Min(minY, screen[vertex * 3 + 1]);
            maxY = Math.Max(maxY, screen[vertex * 3 + 1]);
        }
        return (minX, minY, maxX, maxY);
    }

    private static int[] Rasterize(Model model, float[] screen, IReadOnlyDictionary<int, Texture> textures, int width, int height)
    {
        var pixels = new int[width * height];
        var depth = new float[width * height];
        Array.Fill(depth, float.PositiveInfinity);

        var shades = ClientModelShading.LightModel(model.Positions, model.Indices, model.FaceShadingTypes ?? [], model.Contrast, IconLight);
        var translucent = new List<(int Face, float Depth)>();
        var faceCount = model.Indices.Length / 3;
        for (var face = 0; face < faceCount; face++)
        {
            var alpha = face < model.FaceAlpha.Length ? model.FaceAlpha[face] : 0;
            var type = alpha == 255 ? 2 : alpha == 254 ? 3 : model.FaceShadingTypes is { } types && face < types.Length ? types[face] : 0;
            if (type == 2) continue;
            if (alpha is > 0 and < 254)
            {
                var averageDepth = 0f;
                for (var corner = 0; corner < 3; corner++) averageDepth += screen[model.Indices[face * 3 + corner] * 3 + 2];
                translucent.Add((face, averageDepth));
                continue;
            }
            DrawFace(model, face, type, 1f, screen, shades, textures, pixels, depth, width, height, writeDepth: true);
        }
        translucent.Sort((left, right) => right.Depth.CompareTo(left.Depth));
        foreach (var (face, _) in translucent)
            DrawFace(model, face, model.FaceShadingTypes is { } types && face < types.Length ? types[face] : 0, (255 - model.FaceAlpha[face]) / 255f,
                screen, shades, textures, pixels, depth, width, height, writeDepth: false);
        return pixels;
    }

    /// <summary>Screen X, Y and view depth for each vertex, using the client icon camera.</summary>
    internal static float[] Project(Model model, Camera camera, ZoomMode zoomMode, int width, int height)
    {
        var vertexCount = model.Positions.Length / 3;
        var zoom = zoomMode switch
        {
            ZoomMode.NoteContent => (int)(camera.Zoom * 1.5) << 2,
            ZoomMode.Outlined => (int)(1.04 * camera.Zoom) << 2,
            _ => camera.Zoom << 2,
        };
        var minY = int.MaxValue;
        for (var vertex = 0; vertex < vertexCount; vertex++) minY = Math.Min(minY, model.Positions[vertex * 3 + 1]);
        var pitch = camera.Rotation1 << 3 & 0x3fff;
        float tx = camera.Offset1 << 2;
        float ty = (int)((long)zoom * Sine[pitch] >> 14) - minY / 2 + (camera.Offset2 << 2);
        float tz = (camera.Offset2 << 2) + (int)((long)Cosine[pitch] * zoom >> 14);

        var roll = Radians(-(camera.Zan2d << 3));
        var yaw = Radians(camera.Rotation2 << 3);
        var tilt = Radians(camera.Rotation1 << 3);
        float cr = MathF.Cos(roll), sr = MathF.Sin(roll), cy = MathF.Cos(yaw), sy = MathF.Sin(yaw), ct = MathF.Cos(tilt), st = MathF.Sin(tilt);

        // Uniform scale preserves the client 36×32 framing; the projection centre is (16, 16) with focal length 512.
        var scale = Math.Min(width / (float)ClientWidth, height / (float)ClientHeight);
        var centerX = 16 * scale + (width - ClientWidth * scale) / 2;
        var centerY = 16 * scale + (height - ClientHeight * scale) / 2;
        var focal = 512 * scale;
        var screen = new float[vertexCount * 3];
        for (var vertex = 0; vertex < vertexCount; vertex++)
        {
            float x = model.Positions[vertex * 3], y = model.Positions[vertex * 3 + 1], z = model.Positions[vertex * 3 + 2];
            // Rotate about Z, then Y, translate, then rotate about X (BoundingBox method2473, method2462, method2474, method2462).
            (x, y) = (x * cr - y * sr, x * sr + y * cr);
            (x, z) = (x * cy + z * sy, -x * sy + z * cy);
            x += tx; y += ty; z += tz;
            (y, z) = (y * ct - z * st, y * st + z * ct);
            screen[vertex * 3] = centerX + focal * x / z;
            screen[vertex * 3 + 1] = centerY + focal * y / z;
            screen[vertex * 3 + 2] = z;
        }
        return screen;
    }

    /// <summary>Default text colour and shadow of the quantity label (ItemDefinition.method6279).</summary>
    public const int QuantityColor = unchecked((int)0xFFFFFF00);
    public const int QuantityShadow = unchecked((int)0xFF000001);

    /// <summary>
    /// The quantity label, or null when none is drawn. Without a mode, only stackable items with a positive quantity are labelled.
    /// The client modes are: 0 never, 1 always, and 2 for stackable items or quantities other than one, never for -1
    /// (ItemDefinition.method6279). Formatting follows Class_ta_Sub3.method5686.
    /// </summary>
    public static string? QuantityLabel(int quantity, int? mode, bool stackable)
    {
        var drawn = mode switch
        {
            null => stackable && quantity > 0,
            1 => true,
            2 => (stackable || quantity != 1) && quantity != -1,
            _ => false,
        };
        if (!drawn) return null;
        if (quantity < 100000) return $"<col=ffff00>{quantity}</col>";
        if (quantity < 10000000) return $"<col=ffffff>{quantity / 1000}K</col>";
        return $"<col=00ff80>{quantity / 1000000}M</col>";
    }

    /// <summary>World.method6168: the highest stack variant whose amount the quantity reaches, or the item itself.</summary>
    public static int StackVariant(int itemId, int quantity, int[]? stackIds, int[]? stackAmounts)
    {
        if (quantity <= 1 || stackIds is null || stackAmounts is null) return itemId;
        var variant = -1;
        for (var i = 0; i < Math.Min(10, Math.Min(stackIds.Length, stackAmounts.Length)); i++)
            if (quantity >= stackAmounts[i] && stackAmounts[i] != 0) variant = stackIds[i];
        return variant == -1 ? itemId : variant;
    }

    /// <summary>ItemDefinition.method6284: colours empty pixels next to drawn ones.</summary>
    public static int[] Outline(int[] pixels, int width, int height, int color)
    {
        var result = new int[pixels.Length];
        for (int y = 0, index = 0; y < height; y++)
        for (var x = 0; x < width; x++, index++)
        {
            var value = pixels[index];
            if (value == 0 && ((x > 0 && pixels[index - 1] != 0) || (y > 0 && pixels[index - width] != 0)
                               || (x < width - 1 && pixels[index + 1] != 0) || (y < height - 1 && pixels[index + width] != 0)))
                value = color;
            result[index] = value;
        }
        return result;
    }

    /// <summary>ItemDefinition.method6285: casts a one-pixel shadow down and to the right, in place.</summary>
    public static void Shadow(int[] pixels, int width, int height, int color)
    {
        for (var y = height - 1; y > 0; y--)
        {
            var row = y * width;
            for (var x = width - 1; x > 0; x--)
                if (pixels[row + x] == 0 && pixels[row + x - 1 - width] != 0) pixels[row + x] = color;
        }
    }

    /// <summary>Draws non-transparent source pixels over the destination.</summary>
    public static void Draw(int[] destination, int[] source)
    {
        for (var index = 0; index < destination.Length; index++)
            if (source[index] != 0) destination[index] = source[index];
    }

    /// <summary>The client's final pass: black becomes transparent and everything else opaque.</summary>
    public static void Normalize(int[] pixels)
    {
        for (var index = 0; index < pixels.Length; index++)
            pixels[index] = (pixels[index] & 0xffffff) == 0 ? 0 : pixels[index] | unchecked((int)0xFF000000);
    }

    private static float Radians(int angle) => (angle & 0x3fff) / 16384f * MathF.Tau;

    private static void DrawFace(Model model, int face, int type, float opacity, float[] screen, int[] shades, IReadOnlyDictionary<int, Texture> textures,
        int[] pixels, float[] depth, int width, int height, bool writeDepth)
    {
        int a = model.Indices[face * 3], b = model.Indices[face * 3 + 1], c = model.Indices[face * 3 + 2];
        float x0 = screen[a * 3], y0 = screen[a * 3 + 1], z0 = screen[a * 3 + 2];
        float x1 = screen[b * 3], y1 = screen[b * 3 + 1], z1 = screen[b * 3 + 2];
        float x2 = screen[c * 3], y2 = screen[c * 3 + 1], z2 = screen[c * 3 + 2];
        // Client near plane.
        if (z0 < 50 || z1 < 50 || z2 < 50) return;
        var area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0);
        // Back faces are culled like the client; cache winding is clockwise on the Y-down screen.
        if (area >= -1e-6f) return;

        var textureId = model.FaceTextureIds is { } ids && face < ids.Length ? ids[face] : -1;
        var texture = textureId >= 0 && model.FaceTextureUvs is { } uvs && uvs.Length >= (face + 1) * 6 && textures.TryGetValue(textureId, out var found) ? found : null;
        var packed = model.FaceColors[face];
        Span<int> colors = stackalloc int[3];
        for (var corner = 0; corner < 3; corner++)
        {
            var shade = shades[face * 3 + corner];
            if (type == 3) colors[corner] = 0;
            else if (texture is null) colors[corner] = ClientModelShading.LitFaceColor(packed, shade, textured: false, model.Ambient);
            else
            {
                var intensity = ClientModelShading.ClampLight(shade >> 1);
                var lit = ClientModelShading.LitFaceColor(packed, intensity, textured: true, model.Ambient);
                colors[corner] = Blend(lit, intensity, texture);
            }
        }

        var minX = Math.Max(0, (int)MathF.Floor(MathF.Min(x0, MathF.Min(x1, x2))));
        var maxX = Math.Min(width - 1, (int)MathF.Ceiling(MathF.Max(x0, MathF.Max(x1, x2))));
        var minY = Math.Max(0, (int)MathF.Floor(MathF.Min(y0, MathF.Min(y1, y2))));
        var maxY = Math.Min(height - 1, (int)MathF.Ceiling(MathF.Max(y0, MathF.Max(y1, y2))));
        float iz0 = 1 / z0, iz1 = 1 / z1, iz2 = 1 / z2;
        for (var py = minY; py <= maxY; py++)
        for (var px = minX; px <= maxX; px++)
        {
            float sx = px + 0.5f, sy = py + 0.5f;
            var w0 = ((x1 - sx) * (y2 - sy) - (x2 - sx) * (y1 - sy)) / area;
            var w1 = ((x2 - sx) * (y0 - sy) - (x0 - sx) * (y2 - sy)) / area;
            var w2 = 1 - w0 - w1;
            if (w0 < 0 || w1 < 0 || w2 < 0) continue;
            var inverseDepth = w0 * iz0 + w1 * iz1 + w2 * iz2;
            var z = 1 / inverseDepth;
            var index = py * width + px;
            if (z >= depth[index]) continue;

            float r = 0, g = 0, bl = 0;
            for (var corner = 0; corner < 3; corner++)
            {
                var weight = corner == 0 ? w0 : corner == 1 ? w1 : w2;
                r += (colors[corner] >> 16 & 255) * weight;
                g += (colors[corner] >> 8 & 255) * weight;
                bl += (colors[corner] & 255) * weight;
            }
            if (texture is not null)
            {
                var faceUvs = model.FaceTextureUvs!;
                var offset = face * 6;
                var u = (w0 * faceUvs[offset] * iz0 + w1 * faceUvs[offset + 2] * iz1 + w2 * faceUvs[offset + 4] * iz2) / inverseDepth;
                var v = (w0 * faceUvs[offset + 1] * iz0 + w1 * faceUvs[offset + 3] * iz1 + w2 * faceUvs[offset + 5] * iz2) / inverseDepth;
                var tx = Wrap((int)MathF.Floor(u * texture.Width), texture.Width);
                var ty = Wrap((int)MathF.Floor(v * texture.Height), texture.Height);
                var texel = texture.Argb[ty * texture.Width + tx];
                // Cut-out alpha: transparent texels leave the pixel untouched.
                if ((texel >>> 24) < 128) continue;
                r = r * (texel >> 16 & 255) / 255;
                g = g * (texel >> 8 & 255) / 255;
                bl = bl * (texel & 255) / 255;
            }

            var color = ToRgb(r, g, bl);
            if (opacity < 1)
            {
                var existing = pixels[index];
                color = ToRgb((color >> 16 & 255) * opacity + (existing >> 16 & 255) * (1 - opacity),
                    (color >> 8 & 255) * opacity + (existing >> 8 & 255) * (1 - opacity),
                    (color & 255) * opacity + (existing & 255) * (1 - opacity));
            }
            pixels[index] = color | unchecked((int)0xFF000000);
            if (writeDepth) depth[index] = z;
        }
    }

    // Model_Sub3.method4552: mixes the neutral shade into the lit colour, then applies brightness.
    private static int Blend(int lit, int intensity, Texture texture)
    {
        var result = 0;
        for (var shift = 16; shift >= 0; shift -= 8)
        {
            var channel = lit >> shift & 255;
            var mixed = (channel * (256 - texture.ColorBlend) + 2 * intensity * texture.ColorBlend) >> 8;
            result |= Math.Min(255, Math.Min(65535, mixed * (256 + texture.Brightness)) >> 8) << shift;
        }
        return result;
    }

    private static int Wrap(int value, int size) => ((value % size) + size) % size;

    private static int ToRgb(float r, float g, float b) =>
        Math.Clamp((int)(r + 0.5f), 0, 255) << 16 | Math.Clamp((int)(g + 0.5f), 0, 255) << 8 | Math.Clamp((int)(b + 0.5f), 0, 255);
}
