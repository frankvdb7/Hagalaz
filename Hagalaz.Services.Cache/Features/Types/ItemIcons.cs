using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Logic.Rendering;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Features.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Hagalaz.Services.Cache.Features.Types;

/// <summary>Resolves item definitions, models and textures from the cache and renders client-style inventory icons.</summary>
internal static class ItemIcons
{
    internal readonly record struct Options(int Width, int Height, int Outline, int? Shadow, int Quantity, int? QuantityMode);

    /// <summary>Renders the icon as PNG bytes, or null when the item or a template item has no inventory model.</summary>
    public static byte[]? RenderPng(int id, Options options, ICacheAPI cache, ITypeProvider<IItemType> items)
    {
        var textures = new TextureSource(cache);
        var pixels = Render(id, options.Quantity, options.QuantityMode, noteContent: false, options.Outline, options.Shadow, options, cache, items, textures, depth: 0);
        if (pixels is null) return null;
        using var image = new Image<Rgba32>(options.Width, options.Height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    var argb = pixels[y * options.Width + x];
                    row[x] = new Rgba32((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, (byte)(argb >>> 24));
                }
            }
        });
        using var output = new MemoryStream();
        image.SaveAsPng(output, new PngEncoder { ColorType = PngColorType.RgbWithAlpha });
        return output.ToArray();
    }

    // World.method6168 (stack variant) and ItemDefinition.method6279 (model, passes, templates and quantity label).
    private static int[]? Render(int id, int quantity, int? quantityMode, bool noteContent, int outline, int? shadow, Options options, ICacheAPI cache, ITypeProvider<IItemType> items, TextureSource textures, int depth)
    {
        var item = (ItemType)items.Get(id);
        var variant = ItemIconRenderer.StackVariant(id, quantity, item.StackIds, item.StackAmounts);
        if (variant != id) item = (ItemType)items.Get(variant);
        var appearance = ModelEndpoints.InventoryAppearance(item);
        if (appearance is null || depth > 1) return null;
        var assembled = ModelEndpoints.BuildItemModel(item, appearance, cache);
        var model = new ItemIconRenderer.Model(assembled.Positions, assembled.Indices, assembled.FaceColors, assembled.FaceAlpha, assembled.FaceShadingTypes,
            assembled.FaceTextureIds, assembled.FaceTextureUvs, assembled.Ambient, assembled.Contrast);
        var camera = new ItemIconRenderer.Camera(item.ModelZoom, item.ModelRotation1, item.ModelRotation2, item.Zan2D, item.ModelOffset1, item.ModelOffset2);
        var zoom = noteContent ? ItemIconRenderer.ZoomMode.NoteContent : outline == 2 ? ItemIconRenderer.ZoomMode.Outlined : ItemIconRenderer.ZoomMode.Normal;
        var pixels = ItemIconRenderer.RenderModel(model, camera, textures.For(assembled.FaceTextureIds), zoom, options.Width, options.Height);
        if (outline >= 1) pixels = ItemIconRenderer.Outline(pixels, options.Width, options.Height, ItemIconRenderer.DarkOutline);
        if (outline >= 2) pixels = ItemIconRenderer.Outline(pixels, options.Width, options.Height, ItemIconRenderer.LightOutline);
        if (shadow is { } color and not 0) ItemIconRenderer.Shadow(pixels, options.Width, options.Height, color);

        var result = new int[pixels.Length];
        // Lent and bought items draw the original item first; noted items draw it over the note. Inner items never show a label.
        var underneathId = item.LendTemplateId != -1 ? item.LendId : item.BoughtTemplateId != -1 ? item.BoughtItemId : -1;
        if (underneathId != -1)
        {
            var underneath = Render(underneathId, quantity, quantityMode: 0, noteContent: false, outline, shadow, options, cache, items, textures, depth + 1);
            if (underneath is null) return null;
            ItemIconRenderer.Draw(result, underneath);
        }
        ItemIconRenderer.Draw(result, pixels);
        if (item.NoteTemplateId != -1)
        {
            var content = Render(item.NoteId, quantity: 10, quantityMode: 0, noteContent: true, outline: 1, shadow: null, options, cache, items, textures, depth + 1);
            if (content is null) return null;
            ItemIconRenderer.Draw(result, content);
        }
        if (ItemIconRenderer.QuantityLabel(quantity, quantityMode, item.StackableType == 1) is { } label)
        {
            var scale = Math.Max(1, (int)Math.Min(options.Width / (float)ItemIconRenderer.ClientWidth, options.Height / (float)ItemIconRenderer.ClientHeight));
            textures.Font().Draw(result, options.Width, options.Height, label, 0, 9, ItemIconRenderer.QuantityColor, ItemIconRenderer.QuantityShadow, scale,
                (options.Width - ItemIconRenderer.ClientWidth * scale) / 2, (options.Height - ItemIconRenderer.ClientHeight * scale) / 2);
        }
        ItemIconRenderer.Normalize(result);
        return result;
    }

    /// <summary>Decodes each texture, the material table and the quantity font at most once per icon.</summary>
    private sealed class TextureSource(ICacheAPI cache)
    {
        private readonly Dictionary<int, ItemIconRenderer.Texture?> _textures = [];
        private byte[]? _materials;

        public IReadOnlyDictionary<int, ItemIconRenderer.Texture> For(int[]? faceTextureIds)
        {
            var result = new Dictionary<int, ItemIconRenderer.Texture>();
            foreach (var id in (faceTextureIds ?? []).Where(id => id >= 0).Distinct())
                if (Load(id) is { } texture) result[id] = texture;
            return result;
        }

        private ClientFont? _font;

        /// <summary>The p11_full font the client uses for item quantities (Class472.method5890).</summary>
        public ClientFont Font()
        {
            if (_font is not null) return _font;
            int id;
            try { id = cache.GetFileId(13, "p11_full"); }
            catch (Exception error) when (error is FileNotFoundException or NotSupportedException) { id = -1; }
            if (id < 0) throw new NotSupportedException("The p11_full font is not available in the cache.");
            using var metrics = cache.ReadContainer(13, id);
            using var sprites = cache.ReadContainer(8, id);
            using var spriteData = new MemoryStream(sprites.Data.ToArray());
            using var sprite = new SpriteTypeCodec().Decode(id, spriteData);
            var glyphs = new ClientFont.Glyph[sprite.Image.Frames.Count];
            for (var frame = 0; frame < glyphs.Length; frame++)
            {
                var image = sprite.Image.Frames[frame];
                var mask = new bool[image.Width * image.Height];
                for (var y = 0; y < image.Height; y++)
                for (var x = 0; x < image.Width; x++)
                    mask[y * image.Width + x] = image[x, y].A > 0;
                glyphs[frame] = new ClientFont.Glyph(image.Width, image.Height, mask);
            }
            return _font = new ClientFont(metrics.Data.ToArray(), glyphs);
        }

        private ItemIconRenderer.Texture? Load(int id)
        {
            if (_textures.TryGetValue(id, out var cached)) return cached;
            ItemIconRenderer.Texture? texture = null;
            try
            {
                if (_materials is null)
                {
                    using var materials = cache.Read(26, 0, 0);
                    _materials = materials.ToArray();
                }
                var material = MaterialDefinitionDecoder.Decode(_materials, id);
                using var container = cache.ReadContainer(9, id);
                var (width, height, argb) = TextureImageDecoder.ToArgb(container.Data.ToArray(), material.AlphaMode);
                texture = new ItemIconRenderer.Texture(width, height, argb, material.ColorBlend, material.Brightness);
            }
            catch (Exception error) when (error is FileNotFoundException or NotSupportedException or UnknownImageFormatException or InvalidDataException or EndOfStreamException)
            {
                // A missing or unsupported texture leaves the face in its lit colour, as the model viewer does.
            }
            _textures[id] = texture;
            return texture;
        }
    }
}
