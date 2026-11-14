using System;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Rendering;
using Xunit;

namespace Hagalaz.Cache.Tests.Rendering;

public class TextureSourceTests
{
    // Materials for textures 0 and 2 (alpha modes 0 and 1); texture 1 has none.
    private static byte[] MaterialTable()
    {
        var data = new byte[2 + 3 + 2 * 23];
        data[0] = 0; data[1] = 3;
        data[2] = 1; data[3] = 0; data[4] = 1;
        data[^2] = 0; data[^1] = 1;
        return data;
    }

    private static TextureSource Source(RenderingTestCache cache) => new(cache.Get<ICacheAPI>(),
        cache.Get<ITypeCodec<IMaterialTableType>>(), cache.Get<ISpriteTypeCodec>());

    [Fact]
    public void Material_ReadsTheTableOncePerInstance()
    {
        using var cache = new RenderingTestCache().Archive(TextureSource.MaterialIndex, 0, (0, MaterialTable()));
        var source = Source(cache);

        Assert.Equal(0, source.Material(0).AlphaMode);
        Assert.Equal(1, source.Material(2).AlphaMode);
        Assert.Equal(1, cache.Store.GetReadCount(TextureSource.MaterialIndex, 0));
    }

    [Fact]
    public void Material_RejectsATextureWithoutMaterial()
    {
        using var cache = new RenderingTestCache().Archive(TextureSource.MaterialIndex, 0, (0, MaterialTable()));

        var error = Assert.Throws<NotSupportedException>(() => Source(cache).Material(1));
        Assert.Equal("The texture has no material definition.", error.Message);
    }

    [Fact]
    public void For_LeavesOutMissingTextures()
    {
        using var cache = new RenderingTestCache().Archive(TextureSource.MaterialIndex, 0, (0, MaterialTable()));

        Assert.Empty(Source(cache).For([0, 1, -1]));
    }

    [Fact]
    public void Font_RequiresTheQuantityFont()
    {
        using var cache = new RenderingTestCache();

        var error = Assert.Throws<NotSupportedException>(() => Source(cache).Font());
        Assert.Equal("The p11_full font is not available in the cache.", error.Message);
    }

    [Fact]
    public void Font_LoadsTheQuantityFontOnce()
    {
        using var cache = new RenderingTestCache().Font();
        var source = Source(cache);

        Assert.Same(source.Font(), source.Font());
        Assert.Equal(1, cache.Store.GetReadCount(TextureSource.SpriteIndex, 0));
    }
}
