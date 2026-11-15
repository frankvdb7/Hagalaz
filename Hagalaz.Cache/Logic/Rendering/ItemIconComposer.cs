using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;

namespace Hagalaz.Cache.Logic.Rendering;

/// <summary>How an item icon is drawn: size, outline passes (0-2), shadow colour, quantity and quantity label mode.</summary>
public readonly record struct IconOptions(int Width, int Height, int Outline, int? Shadow, int Quantity, int? QuantityMode);

/// <summary>Draws item inventory icons as the client does: stack variant, templates, outlines, shadow and the quantity label.</summary>
public sealed class ItemIconComposer(
    ICacheAPI cache,
    ModelAssembler assembler,
    ITypeProvider<IItemType> items,
    ITypeCodec<IMaterialTableType> materials,
    ISpriteTypeCodec sprites)
{
    /// <summary>
    /// Every index an icon reads, in a fixed order: item and object definitions, models, sprites, fonts, textures and materials.
    /// Image validators hash these indexes' reference-table versions, so the order is part of the validator.
    /// </summary>
    public static IReadOnlyList<int> SourceIndexes { get; } =
    [
        new ItemTypeData().IndexId, new ObjectTypeData().IndexId, ModelAssembler.ModelIndex, TextureSource.SpriteIndex,
        TextureSource.FontMetricsIndex, TextureSource.TextureIndex, TextureSource.MaterialIndex
    ];

    /// <summary>The icon's ARGB pixels, or <see cref="ModelSubjectAbsentError"/> when the item or a template item has no inventory model.</summary>
    public Result<int[]> Render(int itemId, IconOptions options)
    {
        var textures = new TextureSource(cache, materials, sprites);
        var pixels = Render(itemId, options.Quantity, options.QuantityMode, noteContent: false, options.Outline, options.Shadow, options, textures, depth: 0);
        return pixels is null ? new ModelSubjectAbsentError() : pixels;
    }

    // World.method6168 (stack variant) and ItemDefinition.method6279 (model, passes, templates and quantity label).
    private int[]? Render(int id, int quantity, int? quantityMode, bool noteContent, int outline, int? shadow, IconOptions options, TextureSource textures, int depth)
    {
        var item = (ItemType)items.Get(id);
        var variant = ItemIconRenderer.StackVariant(id, quantity, item.StackIds, item.StackAmounts);
        if (variant != id) item = (ItemType)items.Get(variant);
        var hasInventoryModel = assembler.ItemAppearances(item).Any(value => value.Key == ModelAssembler.InventoryAppearance && value.Available);
        if (!hasInventoryModel || depth > 1) return null;
        var assembled = assembler.Item(item, ModelAssembler.InventoryAppearance).Value.Model;
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
            var underneath = Render(underneathId, quantity, quantityMode: 0, noteContent: false, outline, shadow, options, textures, depth + 1);
            if (underneath is null) return null;
            ItemIconRenderer.Draw(result, underneath);
        }
        ItemIconRenderer.Draw(result, pixels);
        if (item.NoteTemplateId != -1)
        {
            var content = Render(item.NoteId, quantity: 10, quantityMode: 0, noteContent: true, outline: 1, shadow: null, options, textures, depth + 1);
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
}
