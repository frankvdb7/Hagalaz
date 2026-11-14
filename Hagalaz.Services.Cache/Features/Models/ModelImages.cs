using Hagalaz.Cache.Logic.Rendering;
using Hagalaz.Services.Cache.Features.Types;

namespace Hagalaz.Services.Cache.Features.Models;

/// <summary>Renders assembled models and objects to PNG previews, which have no item-style authored icon camera.</summary>
internal static class ModelImages
{
    internal readonly record struct Options(int Width, int Height);
    internal readonly record struct ObjectOptions(int Width, int Height, int? Shape);
    internal readonly record struct WidgetOptions(int Width, int Height, ItemIconRenderer.WidgetCamera Camera);

    public static byte[] RenderPng(AssembledModel assembled, Options options, TextureSource textures, ItemIconRenderer.WidgetCamera? camera = null)
    {
        var model = new ItemIconRenderer.Model(assembled.Positions, assembled.Indices, assembled.FaceColors, assembled.FaceAlpha, assembled.FaceShadingTypes,
            assembled.FaceTextureIds, assembled.FaceTextureUvs, assembled.Ambient, assembled.Contrast);
        var faceTextures = textures.For(assembled.FaceTextureIds);
        var pixels = camera is { } widget
            ? ItemIconRenderer.RenderWidget(model, widget, faceTextures, options.Width, options.Height)
            : ItemIconRenderer.RenderFitted(model, faceTextures, options.Width, options.Height);
        ItemIconRenderer.Normalize(pixels);
        return ItemIcons.EncodePng(pixels, options.Width, options.Height);
    }
}
