using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features;

public static class ModelEndpoints
{
    public static IEndpointRouteBuilder MapModelEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/models/{id:int}", GetModel);
        group.MapGet("/types/items/{id:int}/model", GetItemModel);
        group.MapGet("/types/items/{id:int}/model-appearances", GetItemAppearances);
        group.MapGet("/types/npcs/{id:int}/model", GetNpcModel);
        group.MapGet("/types/objects/{id:int}/model", GetObjectModel);
        group.MapGet("/animation-frames/{id:int}", GetAnimationFrame);
        group.MapGet("/types/animations/{id:int}/sequence", GetAnimationSequence);
        return group;
    }

    private static IResult GetModel(int id, ICacheAPI cache) => ReadModel(id, cache);

    private static IResult GetAnimationSequence(int id, ITypeProvider<IAnimationType> animations)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Animation ID must be nonnegative."));
        var sequence = (AnimationType)animations.Get(id);
        return TypedResults.Ok(new { sequence.Id, FrameIds = sequence.Frames ?? [], FrameDurations = sequence.Delays ?? [], sequence.Loop, sequence.IsTweened });
    }

    private static IResult GetAnimationFrame(int id, ICacheAPI cache)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Frame ID must be nonnegative."));
        using var stream = cache.Read(0, id >> 16, id & 65535);
        var bytes = stream.ToArray();
        if (bytes.Length < 5) throw new System.IO.InvalidDataException("Animation frame header is truncated.");
        var skeletonId = (bytes[1] << 8) | bytes[2];
        using var skeletonContainer = cache.ReadContainer(1, skeletonId);
        var skeleton = AnimationFrameDecoder.DecodeSkeleton(skeletonContainer.Data.ToArray());
        return TypedResults.Ok(new { Frame = AnimationFrameDecoder.DecodeFrame(bytes, skeleton), Skeleton = skeleton });
    }

    private static IResult GetItemAppearances(int id, ITypeProvider<IItemType> items)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Item ID must be nonnegative."));
        return TypedResults.Ok(ItemAppearances(items.Get(id)));
    }

    private static ItemAppearance[] ItemAppearances(IItemType item) =>
    [
        new("inventory", "Inventory", Parts(item.InterfaceModelId)),
        new("male-equipped", "Male equipped", Parts(item.MaleWornModelId1, item.MaleWornModelId2, item.MaleWornModelId3), item.MaleWearXOffset, item.MaleWearYOffset, item.MaleWearZOffset),
        new("female-equipped", "Female equipped", Parts(item.FemaleWornModelId1, item.FemaleWornModelId2, item.FemaleWornModelId3), item.FemaleWearXOffset, item.FemaleWearYOffset, item.FemaleWearZOffset),
        new("male-head", "Male head", Parts(item.MaleHeadModel, item.MaleHeadModel2)),
        new("female-head", "Female head", Parts(item.FemaleHeadModel, item.FemaleHeadModel2)),
    ];

    // The client ignores secondary/tertiary references after the first absent part.
    private static int[] Parts(params int[] ids) => ids.TakeWhile(id => id >= 0).ToArray();

    public sealed record ItemAppearance(string Key, string Label, int[] ModelIds, int OffsetX = 0, int OffsetY = 0, int OffsetZ = 0)
    {
        public bool Available => ModelIds.Length > 0;
    }

    private static IResult GetItemModel(int id, string? appearance, ICacheAPI cache, ITypeProvider<IItemType> items)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Item ID must be nonnegative."));
        var item = (ItemType)items.Get(id);
        var selected = ItemAppearances(item).FirstOrDefault(value => value.Key == (appearance ?? "inventory"));
        if (selected is null) return TypedResults.BadRequest(EndpointErrors.Validation("Unknown item model appearance."));
        if (!selected.Available) return TypedResults.NotFound();
        var inventory = selected.Key == "inventory";
        // Item offsets have already been shifted by two in the definition codec; assembly offsets use unshifted units.
        return AssembleModel(selected.ModelIds[0], selected.ModelIds, cache, inventory ? item.ScaleX : 128, inventory ? item.ScaleY : 128, inventory ? item.ScaleZ : 128,
            selected.OffsetX / 4, selected.OffsetY / 4, selected.OffsetZ / 4,
            Pairs(item.OriginalModelColors), Pairs(item.ModifiedModelColors), Pairs(item.OriginalTextureColors), Pairs(item.ModifiedTextureColors), [],
            $"{selected.Label} model with definition recolors and retextures. Equipped/head models are isolated item parts, without a player body or customization. Client lighting and transparency ordering are not yet supported."
                + (item.UnknownArray1 is { Length: > 0 } ? " Palette-index recolor overrides are not yet supported." : ""));

        static short[]? Pairs(int[]? values) => values?.Select(value => (short)value).ToArray();
    }

    private static IResult ReadModel(int id, ICacheAPI cache)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Model ID must be nonnegative."));
        using var container = cache.ReadContainer(7, id);
        try
        {
            var geometry = ModelGeometryDecoder.Decode(container.Data.ToArray());
            return TypedResults.Ok(new ModelResponse(id, geometry.Positions, geometry.Indices, geometry.FaceColors, geometry.FaceAlpha, geometry.VertexSkins,
                new[] { "Geometry, projected textures and ordinary transparency preview. Client lighting and transparency ordering are not yet supported." }, CoordinateScale: geometry.Version < 13 ? 0.25f : 1, FaceSkins: geometry.FaceSkins, FaceTextureIds: geometry.FaceTextureIds, FaceTextureVertices: geometry.FaceTextureVertices, FaceTextureUvs: geometry.FaceTextureUvs, Particles: ReadParticles(cache, geometry.Emitters ?? [])));
        }
        catch (NotSupportedException exception)
        {
            return TypedResults.Problem(title: "Unsupported model format", detail: exception.Message, statusCode: 422);
        }
    }

    private static IResult GetNpcModel(int id, ICacheAPI cache, ITypeProvider<INpcType> npcs)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("NPC ID must be nonnegative."));
        var npc = (NpcType)npcs.Get(id);
        if (npc.ModelIDs is not { Length: > 0 }) return TypedResults.NotFound();
        var animationIds = new List<int> { npc.IdleAnimationId, npc.MoveType1AnimationId, npc.MoveType2AnimationId, npc.RunAnimationId };
        var animationWarning = "";
        if (npc.RenderId >= 0)
        {
            try
            {
                using var renderSet = cache.Read(2, 32, npc.RenderId);
                animationIds.AddRange(RenderAnimationSetDecoder.Decode(renderSet));
            }
            catch (Exception exception) when (exception is System.IO.IOException or NotSupportedException)
            {
                animationWarning = " Render-animation associations could not be decoded: " + exception.Message;
            }
        }
        return AssembleModel(id, npc.ModelIDs, cache, 128, 128, 128, 0, 0, 0,
            npc.OriginalColours, npc.ModifiedColours, npc.AShortArray1508, npc.AShortArray1491, animationIds.ToArray(),
            "Base NPC model preview with projected textures. Variant transforms, per-part offsets, client lighting are not yet supported." + animationWarning, [npc.ScaleX / 128f, npc.ScaleY / 128f, npc.ScaleX / 128f]);
    }

    private static IResult GetObjectModel(int id, int? shape, ICacheAPI cache, ITypeProvider<IObjectType> objects)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Object ID must be nonnegative."));
        var definition = (ObjectType)objects.Get(id);
        if (definition.Shapes is not { Length: > 0 }) return TypedResults.NotFound();
        var selectedShape = shape ?? definition.Shapes[0];
        var group = Array.FindIndex(definition.Shapes, value => value == selectedShape);
        if (group < 0 || group >= definition.ModelIDs.Length) return TypedResults.NotFound();
        if (definition.Inverted) return TypedResults.Problem(title: "Unsupported object appearance", detail: "Inverted object models are not yet supported.", statusCode: 422);
        return AssembleModel(id, definition.ModelIDs[group], cache, definition.ScaleX, definition.ScaleY, definition.ScaleZ,
            definition.OffsetX, definition.OffsetY, definition.OffsetZ, definition.OriginalColors, definition.ModifiedColors, definition.AShortArray831, definition.AShortArray762, definition.AnimationIDs ?? [],
            $"Object shape {selectedShape} preview with projected textures. Placement rotation, contouring, variant transforms, client lighting are not yet supported.");
    }

    private static IResult AssembleModel(int id, int[] modelIds, ICacheAPI cache, int scaleX, int scaleY, int scaleZ,
        int offsetX, int offsetY, int offsetZ, short[]? originalColors, short[]? replacementColors, short[]? originalTextures, short[]? replacementTextures, int[] animationIds, string warning, float[]? scale = null)
    {
        var positions = new List<int>();
        var indices = new List<int>();
        var colors = new List<int>();
        var alpha = new List<int>();
        var skins = new List<int>();
        var faceSkins = new List<int>();
        var textureIds = new List<int>();
        var textureVertices = new List<int>();
        var textureUvs = new List<float>();
        var emitters = new List<ModelGeometryDecoder.EmitterAttachment>();
        try
        {
            foreach (var modelId in modelIds.Where(modelId => modelId >= 0))
            {
                using var container = cache.ReadContainer(7, modelId);
                var part = ModelGeometryDecoder.Decode(container.Data.ToArray());
                var vertexOffset = positions.Count / 3;
                var faceOffset = colors.Count;
                emitters.AddRange((part.Emitters ?? []).Select(emitter => emitter with { Face = emitter.Face + faceOffset }));
                for (var coordinate = 0; coordinate < part.Positions.Length; coordinate++)
                {
                    var axis = coordinate % 3;
                    var axisScale = axis == 0 ? scaleX : axis == 1 ? scaleY : scaleZ;
                    var offset = axis == 0 ? offsetX : axis == 1 ? offsetY : offsetZ;
                    var unitScale = part.Version < 13 ? 4 : 1;
                    positions.Add(((part.Positions[coordinate] * unitScale * axisScale) >> 7) + offset * 4);
                }
                indices.AddRange(part.Indices.Select(index => index + vertexOffset));
                colors.AddRange(part.FaceColors);
                alpha.AddRange(part.FaceAlpha);
                faceSkins.AddRange(part.FaceSkins ?? Enumerable.Repeat(-1, part.FaceColors.Length).ToArray());
                textureIds.AddRange(part.FaceTextureIds ?? Enumerable.Repeat(-1, part.FaceColors.Length).ToArray());
                textureVertices.AddRange((part.FaceTextureVertices ?? Enumerable.Repeat(-1, part.Indices.Length).ToArray()).Select(vertex => vertex < 0 ? -1 : vertex + vertexOffset));
                textureUvs.AddRange(part.FaceTextureUvs ?? new float[part.Indices.Length * 2]);
                skins.AddRange(part.VertexSkins ?? Enumerable.Repeat(-1, part.Positions.Length / 3).ToArray());
            }
            ReplaceValues(colors, originalColors, replacementColors);
            ReplaceValues(textureIds, originalTextures, replacementTextures);
            for (var face = 0; face < textureIds.Count; face++)
                if (textureIds[face] == 65535) textureIds[face] = -1;
            return TypedResults.Ok(new ModelResponse(id, positions.ToArray(), indices.ToArray(), colors.ToArray(), alpha.ToArray(), skins.Any(skin => skin >= 0) ? skins.ToArray() : null,
                new[] { warning }, animationIds.Where(animationId => animationId >= 0).Distinct().ToArray(), 1, faceSkins.ToArray(), textureIds.ToArray(), textureVertices.ToArray(), textureUvs.ToArray(), scale, ReadParticles(cache, emitters)));
        }
        catch (NotSupportedException exception)
        {
            return TypedResults.Problem(title: "Unsupported model format", detail: exception.Message, statusCode: 422);
        }
    }

    private static void ReplaceValues(List<int> values, short[]? originals, short[]? replacements)
    {
        if (originals is null || replacements is null) return;
        // Definition order matters: later pairs may replace a value produced by an earlier pair.
        for (var pair = 0; pair < Math.Min(originals.Length, replacements.Length); pair++)
            for (var face = 0; face < values.Count; face++)
                if (values[face] == (originals[pair] & 65535)) values[face] = replacements[pair] & 65535;
    }

    private static ModelParticle[] ReadParticles(ICacheAPI cache, IEnumerable<ModelGeometryDecoder.EmitterAttachment> attachments)
    {
        var result = new List<ModelParticle>();
        foreach (var group in attachments.GroupBy(attachment => attachment.EmitterId))
        {
            ParticleEmitterDecoder.Definition? definition = null;
            string? warning = null;
            try
            {
                var member = cache.ReadReferenceTable(27).GetEntry(0)?.GetEntry(group.Key)
                    ?? throw new System.IO.FileNotFoundException($"Particle emitter #{group.Key} is absent from the cache reference table.");
                using var data = cache.Read(27, 0, member.Index);
                definition = ParticleEmitterDecoder.Decode(data.ToArray());
            }
            catch (Exception exception) when (exception is System.IO.IOException or NotSupportedException)
            {
                warning = $"Particle emitter #{group.Key} is unavailable: {exception.Message}";
            }
            result.AddRange(group.Select(attachment => new ModelParticle(attachment.EmitterId, attachment.Face, definition, warning)));
        }
        return result.ToArray();
    }

    public sealed record ModelParticle(int EmitterId, int Face, ParticleEmitterDecoder.Definition? Definition, string? Warning);
    public sealed record ModelResponse(int Id, int[] Positions, int[] Indices, int[] FaceColors, int[] FaceAlpha, int[]? VertexSkins, string[] Warnings, int[]? AnimationIds = null, float CoordinateScale = 1, int[]? FaceSkins = null, int[]? FaceTextureIds = null, int[]? FaceTextureVertices = null, float[]? FaceTextureUvs = null, float[]? Scale = null, ModelParticle[]? Particles = null);
}
