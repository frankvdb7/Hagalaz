using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Logic.Rendering;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Features.Models.Dtos;
using Hagalaz.Services.Cache.Features.Types;

namespace Hagalaz.Services.Cache.Features.Models;

/// <summary>Renders cached models and objects to PNG previews, which have no item-style authored icon camera.</summary>
internal static class ModelImages
{
    internal readonly record struct Options(int Width, int Height);
    internal readonly record struct ObjectOptions(int Width, int Height, int? Shape);

    public static byte[] RenderPng(int id, Options options, ICacheAPI cache) =>
        Render(ModelEndpoints.BuildModel(id, [id], cache, 128, 128, 128, 0, 0, 0, null, null, null, null, [], ""), options, cache);

    /// <summary>Applies the definition's scale and recolours; offsets are irrelevant because the preview is centred.</summary>
    public static byte[] RenderObjectPng(ObjectType definition, int[] modelIds, Options options, ICacheAPI cache) =>
        Render(ModelEndpoints.BuildModel(definition.Id, modelIds, cache, definition.ScaleX, definition.ScaleY, definition.ScaleZ, 0, 0, 0,
            definition.OriginalColors, definition.ModifiedColors, definition.AShortArray831, definition.AShortArray762, [], "",
            ambient: 64 + definition.Ambient, contrast: 850 + definition.Contrast), options, cache);

    private static byte[] Render(GetModelDto assembled, Options options, ICacheAPI cache)
    {
        var model = new ItemIconRenderer.Model(assembled.Positions, assembled.Indices, assembled.FaceColors, assembled.FaceAlpha, assembled.FaceShadingTypes,
            assembled.FaceTextureIds, assembled.FaceTextureUvs, assembled.Ambient, assembled.Contrast);
        var textures = new ItemIcons.TextureSource(cache);
        var pixels = ItemIconRenderer.RenderFitted(model, textures.For(assembled.FaceTextureIds), options.Width, options.Height);
        ItemIconRenderer.Normalize(pixels);
        return ItemIcons.EncodePng(pixels, options.Width, options.Height);
    }
}
