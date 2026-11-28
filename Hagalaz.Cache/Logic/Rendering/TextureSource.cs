using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using SixLabors.ImageSharp;

namespace Hagalaz.Cache.Logic.Rendering;

/// <summary>
/// Reads the textures, material table and quantity font one render needs, each at most once per instance.
/// Create one per render: it does not observe cache writes.
/// </summary>
public sealed class TextureSource(ICacheAPI cache, ITypeCodec<IMaterialTableType> materialCodec, ISpriteTypeCodec spriteCodec)
{
    /// <summary>The index holding texture images, one container per texture ID.</summary>
    public const int TextureIndex = 9;

    /// <summary>The index holding the material table, stored as member 0 of archive 0.</summary>
    public const int MaterialIndex = 26;

    /// <summary>The index holding font metrics.</summary>
    public const int FontMetricsIndex = 13;

    /// <summary>The index holding sprites, including font glyph sheets.</summary>
    public const int SpriteIndex = 8;

    private const string QuantityFontName = "p11_full";

    private readonly Dictionary<int, ItemIconRenderer.Texture?> _textures = [];
    private IMaterialTableType? _materials;
    private ClientFont? _font;

    /// <summary>The material for <paramref name="textureId"/>; throws <see cref="NotSupportedException"/> as <see cref="MaterialTableType.Require"/> does.</summary>
    public MaterialEntry Material(int textureId) => MaterialTableType.Require(Materials(), textureId);

    /// <summary>The renderable textures among <paramref name="faceTextureIds"/>; missing or unsupported textures are left out.</summary>
    public IReadOnlyDictionary<int, ItemIconRenderer.Texture> For(int[]? faceTextureIds)
    {
        var result = new Dictionary<int, ItemIconRenderer.Texture>();
        foreach (var id in (faceTextureIds ?? []).Where(id => id >= 0).Distinct())
            if (Load(id) is { } texture) result[id] = texture;
        return result;
    }

    /// <summary>The p11_full font the client uses for item quantities (Class472.method5890).</summary>
    public ClientFont Font()
    {
        if (_font is not null) return _font;
        int id;
        try { id = cache.GetFileId(FontMetricsIndex, QuantityFontName); }
        catch (Exception error) when (error is FileNotFoundException or NotSupportedException) { id = -1; }
        if (id < 0) throw new NotSupportedException("The p11_full font is not available in the cache.");
        using var metrics = cache.ReadContainer(FontMetricsIndex, id);
        using var sprites = cache.ReadContainer(SpriteIndex, id);
        using var spriteData = new MemoryStream(sprites.Data.ToArray());
        using var sprite = spriteCodec.Decode(id, spriteData);
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

    private IMaterialTableType Materials()
    {
        if (_materials is not null) return _materials;
        using var materials = cache.Read(MaterialIndex, 0, 0);
        return _materials = materialCodec.Decode(0, materials);
    }

    private ItemIconRenderer.Texture? Load(int id)
    {
        if (_textures.TryGetValue(id, out var cached)) return cached;
        ItemIconRenderer.Texture? texture = null;
        try
        {
            var material = Material(id);
            using var container = cache.ReadContainer(TextureIndex, id);
            var (width, height, argb) = TextureImageConverter.ToArgb(container.Data.ToArray(), material.AlphaMode);
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
