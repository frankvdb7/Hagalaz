using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Features.Shared;
using Hagalaz.Services.Cache.Features.Models.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;

namespace Hagalaz.Services.Cache.Features.Models;

public static class ModelEndpoints
{
    private static string[] ProjectionWarnings(int[]? faces) => faces is { Length: > 0 }
        ? [$"First-texel texture fallback on {faces.Length} faces with invalid projection coordinates."] : [];
    public static IEndpointRouteBuilder MapModelEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/models/{id:int}", GetModel)
            .WithSummary("Decode a model's geometry").Produces<GetModelDto>();
        group.MapGet("/models/capacity", (ICacheAPI cache) => TypedResults.Ok(new GetModelCapacityDto(cache.ReadReferenceTable(7).Capacity)))
            .WithSummary("Get the model index capacity");
        group.MapGet("/models/range", GetModelRange)
            .WithSummary("List model IDs in a range of at most 200").Produces<GetModelSummaryDto[]>();
        group.MapGet("/types/items/{id:int}/model", GetItemModel)
            .WithSummary("Assemble an item appearance model").Produces<GetModelDto>();
        group.MapGet("/types/items/{id:int}/model-appearances", GetItemAppearances)
            .WithSummary("List an item's model appearances").Produces<GetItemModelAppearanceDto[]>();
        group.MapGet("/types/npcs/{id:int}/model", GetNpcModel)
            .WithSummary("Assemble an NPC model").Produces<GetModelDto>();
        group.MapGet("/types/objects/{id:int}/model", GetObjectModel)
            .WithSummary("Assemble an object model for one shape").Produces<GetModelDto>();
        group.MapGet("/types/objects/{id:int}/render-appearance", GetObjectRenderAppearance)
            .WithSummary("Assemble an object as placed in a map region").Produces<GetObjectRenderAppearanceDto>();
        group.MapPost("/types/objects/render-appearances", GetObjectRenderAppearances)
            .WithSummary("Assemble up to 32 object placements, each with its own status").Produces<ObjectRenderAppearanceResultDto[]>();
        group.MapGet("/animation-frames/{id:int}", GetAnimationFrame)
            .WithSummary("Decode an animation frame and its skeleton").Produces<GetAnimationFrameDto>();
        group.MapGet("/types/animations/{id:int}/sequence", GetAnimationSequence)
            .WithSummary("Get an animation's frame sequence").Produces<GetAnimationSequenceDto>();
        return group;
    }

    private static IResult GetModel(int id, ICacheAPI cache) => ReadModel(id, cache);

    private static IResult GetModelRange(int startId, int endIdExclusive, ICacheAPI cache)
    {
        if (startId < 0 || endIdExclusive < startId || (long)endIdExclusive - startId > 200)
            return TypedResults.BadRequest(EndpointErrors.Validation("Enter a model ID range of at most 200 IDs."));
        var table = cache.ReadReferenceTable(7);
        return TypedResults.Ok(table.Entries.Select(entry => entry.Key).Where(id => id >= startId && id < endIdExclusive)
            .OrderBy(id => id).Select(id => new GetModelSummaryDto(id)).ToArray());
    }

    private static IResult GetAnimationSequence(int id, ITypeProvider<IAnimationType> animations)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Animation ID must be nonnegative."));
        var sequence = (AnimationType)animations.Get(id);
        return TypedResults.Ok(new GetAnimationSequenceDto(sequence.Id, sequence.Frames ?? [], sequence.Delays ?? [], sequence.Loop, sequence.IsTweened));
    }

    private static IResult GetAnimationFrame(int id, ICacheAPI cache)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Frame ID must be nonnegative."));
        using var stream = ReadMember(cache, 0, id >> 16, id & 65535);
        var bytes = stream.ToArray();
        if (bytes.Length < 5) throw new System.IO.InvalidDataException("Animation frame header is truncated.");
        var skeletonId = (bytes[1] << 8) | bytes[2];
        using var skeletonContainer = cache.ReadContainer(1, skeletonId);
        var skeleton = AnimationFrameDecoder.DecodeSkeleton(skeletonContainer.Data.ToArray());
        var frame = AnimationFrameDecoder.DecodeFrame(bytes, skeleton);
        return TypedResults.Ok(new GetAnimationFrameDto(
            new AnimationFrameDto(frame.SkeletonId, frame.Transforms.Select(transform =>
                new AnimationTransformDto(transform.Slot, transform.X, transform.Y, transform.Z, transform.PivotSlot, transform.TweenFlags)).ToArray()),
            new AnimationSkeletonDto(skeleton.Types, skeleton.Enabled, skeleton.Masks, skeleton.Groups)));
    }

    private static IResult GetItemAppearances(int id, ITypeProvider<IItemType> items)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Item ID must be nonnegative."));
        return TypedResults.Ok(ItemAppearances(items.Get(id)));
    }

    private static GetItemModelAppearanceDto[] ItemAppearances(IItemType item) =>
    [
        new("inventory", "Inventory", Parts(item.InterfaceModelId)),
        new("male-equipped", "Male equipped", Parts(item.MaleWornModelId1, item.MaleWornModelId2, item.MaleWornModelId3), item.MaleWearXOffset, item.MaleWearYOffset, item.MaleWearZOffset),
        new("female-equipped", "Female equipped", Parts(item.FemaleWornModelId1, item.FemaleWornModelId2, item.FemaleWornModelId3), item.FemaleWearXOffset, item.FemaleWearYOffset, item.FemaleWearZOffset),
        new("male-head", "Male head", Parts(item.MaleHeadModel, item.MaleHeadModel2)),
        new("female-head", "Female head", Parts(item.FemaleHeadModel, item.FemaleHeadModel2)),
    ];

    // The client ignores secondary/tertiary references after the first absent part.
    private static int[] Parts(params int[] ids) => ids.TakeWhile(id => id >= 0).ToArray();

    private static IResult GetItemModel(int id, string? appearance, ICacheAPI cache, ITypeProvider<IItemType> items)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Item ID must be nonnegative."));
        var item = (ItemType)items.Get(id);
        var selected = ItemAppearances(item).FirstOrDefault(value => value.Key == (appearance ?? "inventory"));
        if (selected is null) return TypedResults.BadRequest(EndpointErrors.Validation("Unknown item model appearance."));
        if (!selected.Available) return TypedResults.NotFound();
        return TypedResults.Ok(BuildItemModel(item, selected, cache));
    }

    /// <summary>The item's inventory appearance, or null when it has no inventory model.</summary>
    internal static GetItemModelAppearanceDto? InventoryAppearance(ItemType item) =>
        ItemAppearances(item).FirstOrDefault(value => value.Key == "inventory" && value.Available);

    /// <summary>Assembles an item appearance with definition scale, recolours and retextures, shared by previews and rendered icons.</summary>
    internal static GetModelDto BuildItemModel(ItemType item, GetItemModelAppearanceDto selected, ICacheAPI cache)
    {
        var inventory = selected.Key == "inventory";
        // Item offsets have already been shifted by two in the definition codec; assembly offsets use unshifted units.
        return BuildModel(selected.ModelIds[0], selected.ModelIds, cache, inventory ? item.ScaleX : 128, inventory ? item.ScaleY : 128, inventory ? item.ScaleZ : 128,
            selected.OffsetX / 4, selected.OffsetY / 4, selected.OffsetZ / 4,
            Pairs(item.OriginalModelColors), Pairs(item.ModifiedModelColors), Pairs(item.OriginalTextureColors), Pairs(item.ModifiedTextureColors), [],
            $"{selected.Label} model with definition recolors and retextures. Equipped/head models are isolated item parts, without a player body or customization. Transparent faces use client priorities and depth ordering within each priority. Intersecting triangles and full client rendering parity remain unverified. Client model lighting uses a fixed inspection light; map atmosphere and seam lighting remain unverified."
                + (item.UnknownArray1 is { Length: > 0 } ? " Palette-index recolor overrides are not yet supported." : ""), ambient: 64 + item.Ambient, contrast: (inventory ? 768 : 850) + item.Contrast);

        static short[]? Pairs(int[]? values) => values?.Select(value => (short)value).ToArray();
    }

    private static IResult ReadModel(int id, ICacheAPI cache)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Model ID must be nonnegative."));
        using var container = cache.ReadContainer(7, id);
        var geometry = ModelGeometryDecoder.Decode(container.Data.ToArray(), allowFirstTexelFallback: true);
        return TypedResults.Ok(new GetModelDto(id, geometry.Positions, geometry.Indices, geometry.FaceColors, geometry.FaceAlpha, geometry.VertexSkins,
                new[] { "Geometry, projected textures and ordinary transparency preview. Transparent faces use client priorities and depth ordering within each priority. Intersecting triangles and full client rendering parity remain unverified. Client model lighting uses a fixed inspection light; map atmosphere and seam lighting remain unverified." }.Concat(ProjectionWarnings(geometry.FirstTexelFaces)).ToArray(), CoordinateScale: geometry.Version < 13 ? 0.25f : 1, FaceSkins: geometry.FaceSkins, FaceTextureIds: geometry.FaceTextureIds, FaceTextureVertices: geometry.FaceTextureVertices, FaceTextureUvs: geometry.FaceTextureUvs, Particles: ReadParticles(cache, geometry.Emitters ?? []), FacePriorities: geometry.FacePriorities, FaceShadingTypes: geometry.FaceShadingTypes, FirstTexelFaces: geometry.FirstTexelFaces));
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
                using var renderSet = ReadMember(cache, 2, 32, npc.RenderId);
                animationIds.AddRange(RenderAnimationSetDecoder.Decode(renderSet));
            }
            catch (Exception exception) when (exception is System.IO.IOException or NotSupportedException)
            {
                animationWarning = " Render-animation associations could not be decoded: " + exception.Message;
            }
        }
        return AssembleModel(id, npc.ModelIDs, cache, 128, 128, 128, 0, 0, 0,
            npc.OriginalColours, npc.ModifiedColours, npc.AShortArray1508, npc.AShortArray1491, animationIds.ToArray(),
            "Base NPC model preview with projected textures. Variant transforms, per-part offsets are not yet supported. Client model lighting uses a fixed inspection light; map atmosphere and seam lighting remain unverified." + animationWarning, [npc.ScaleX / 128f, npc.ScaleY / 128f, npc.ScaleX / 128f], 64 + npc.LightModifier, 850 + npc.ShadowModifier);
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
            $"Object shape {selectedShape} preview with projected textures. Placement rotation, contouring, variant transforms are not yet supported. Client model lighting uses a fixed inspection light; map atmosphere and seam lighting remain unverified.", ambient: 64 + definition.Ambient, contrast: 850 + definition.Contrast);
    }

    private static IResult GetObjectRenderAppearances(ObjectRenderAppearanceKeyDto?[]? request, ICacheAPI cache, ITypeProvider<IObjectType> objects, CancellationToken cancellationToken)
    {
        if (request is null || request.Length is < 1 or > 32 || request.Any(key => key is null || key.Id < 0 || key.Shape is < 0 or > 22 || key.Rotation is < 0 or > 3))
            return TypedResults.BadRequest(EndpointErrors.Validation("Enter 1..32 appearance keys with nonnegative IDs, shapes 0..22 and rotations 0..3."));
        var rows = new List<ObjectRenderAppearanceResultDto>();
        foreach (var key in request.OfType<ObjectRenderAppearanceKeyDto>().Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            IResult result;
            try { result = GetObjectRenderAppearance(key.Id, key.Shape, key.Rotation, cache, objects); }
            catch (Exception error) when (error is not OperationCanceledException) { result = EndpointErrors.MapException(error); }
            var status = (result as IStatusCodeHttpResult)?.StatusCode ?? 500;
            var appearance = (result as Ok<GetObjectRenderAppearanceDto>)?.Value;
            var problem = (result as ProblemHttpResult)?.ProblemDetails ?? (result as IValueHttpResult)?.Value as ProblemDetails;
            if (appearance is null) problem ??= new ProblemDetails { Status = status, Title = "Object appearance unavailable." };
            rows.Add(new ObjectRenderAppearanceResultDto(key.Id, key.Shape, key.Rotation, status, appearance, problem));
        }
        return TypedResults.Ok(rows.ToArray());
    }

    private static IResult GetObjectRenderAppearance(int id, int shape, int rotation, ICacheAPI cache, ITypeProvider<IObjectType> objects)
    {
        if (id < 0 || shape is < 0 or > 22 || rotation is < 0 or > 3)
            return TypedResults.BadRequest(EndpointErrors.Validation("Enter a nonnegative object ID, shape 0..22 and rotation 0..3."));
        var definition = (ObjectType)objects.Get(id);
        var name = definition.Name;
        var modelShape = shape is >= 4 and <= 8 ? 4 : shape == 11 ? 10 : shape;
        var warnings = new List<string>();
        if (definition.TransformToIDs is { Length: > 0 } variants)
        {
            // Match ObjectDefinition.getTransformToDefinition with zero-valued static preview state.
            var hasSelector = definition.VarpBitFileId >= 0 || definition.VarpFileId >= 0;
            var selected = hasSelector && variants.Length > 1 ? variants[0] : variants[^1];
            var state = hasSelector ? "assumed zero-valued varp/varbit state" : "the definition's default transform fallback";
            if (selected == -1)
                return TypedResults.Ok(new GetObjectRenderAppearanceDto(id, shape, rotation, definition.SizeX, definition.SizeY, definition.DecorDisplacement,
                    definition.GroundContoured, definition.AnInt780, [], [$"Hidden for {state}; live game state is unavailable."], name));
            definition = (ObjectType)objects.Get(selected);
            warnings.Add($"Static preview uses object {selected} for {state}; live game state is unavailable.");
        }
        var group = Array.FindIndex(definition.Shapes ?? [], value => value == modelShape);
        if (group < 0 || group >= definition.ModelIDs.Length) return TypedResults.NotFound();
        if (definition.AByteArray816 is { Length: > 0 }) warnings.Add("Palette-index recolor overrides are unavailable.");
        if (definition.AByte787 != 0) warnings.Add("The definition's additional color adjustment is unavailable.");
        if (definition.GroundContoured > 2) warnings.Add($"Terrain contouring mode {definition.GroundContoured} is unavailable.");
        var orientations = shape == 2 ? new[] { rotation + 4, (rotation + 1) & 3 }
            : shape == 8 ? new[] { rotation + 4, ((rotation + 2) & 3) + 4 }
            : new[] { shape == 6 ? rotation + 4 : shape == 7 ? ((rotation + 2) & 3) + 4 : rotation };
        var parts = new List<GetModelDto>();
        foreach (var orientation in orientations)
        {
            var mirrored = definition.Inverted || (modelShape == 2 && orientation > 3);
            var model = BuildModel(id, definition.ModelIDs[group], cache, definition.ScaleX, definition.ScaleY, definition.ScaleZ,
                definition.OffsetX / 4, definition.OffsetY / 4, definition.OffsetZ / 4,
                definition.OriginalColors, definition.ModifiedColors, definition.AShortArray831, definition.AShortArray762, [],
                "Static region appearance. Scene atmosphere, animated variants and advanced lighting are not simulated.",
                ambient: 64 + definition.Ambient, contrast: 850 + definition.Contrast,
                placement: (modelShape, orientation, mirrored), extraRotation: shape == 11);
            parts.Add(model);
            warnings.AddRange(ProjectionWarnings(model.FirstTexelFaces));
        }
        return TypedResults.Ok(new GetObjectRenderAppearanceDto(id, shape, rotation, definition.SizeX, definition.SizeY, definition.DecorDisplacement,
            definition.GroundContoured, definition.AnInt780, parts.ToArray(), warnings.ToArray(), name));
    }

    private static IResult AssembleModel(int id, int[] modelIds, ICacheAPI cache, int scaleX, int scaleY, int scaleZ,
        int offsetX, int offsetY, int offsetZ, short[]? originalColors, short[]? replacementColors, short[]? originalTextures, short[]? replacementTextures, int[] animationIds, string warning, float[]? scale = null, int ambient = 64, int contrast = 850,
        (int Shape, int Orientation, bool Mirrored)? placement = null, bool extraRotation = false)
        => TypedResults.Ok(BuildModel(id, modelIds, cache, scaleX, scaleY, scaleZ, offsetX, offsetY, offsetZ, originalColors, replacementColors, originalTextures, replacementTextures,
            animationIds, warning, scale, ambient, contrast, placement, extraRotation));

    /// <summary>Client model assembly shared by model previews and rendered item icons. Throws <see cref="NotSupportedException"/> for undecodable models.</summary>
    internal static GetModelDto BuildModel(int id, int[] modelIds, ICacheAPI cache, int scaleX, int scaleY, int scaleZ,
        int offsetX, int offsetY, int offsetZ, short[]? originalColors, short[]? replacementColors, short[]? originalTextures, short[]? replacementTextures, int[] animationIds, string warning, float[]? scale = null, int ambient = 64, int contrast = 850,
        (int Shape, int Orientation, bool Mirrored)? placement = null, bool extraRotation = false)
    {
        var positions = new List<int>();
        var indices = new List<int>();
        var colors = new List<int>();
        var alpha = new List<int>();
        var priorities = new List<int>();
        var shadingTypes = new List<int>();
        var skins = new List<int>();
        var faceSkins = new List<int>();
        var textureIds = new List<int>();
        var textureVertices = new List<int>();
        var textureUvs = new List<float>();
        var firstTexelFaces = new List<int>();
        var emitters = new List<ModelGeometryDecoder.EmitterAttachment>();
        {
            foreach (var modelId in modelIds.Where(modelId => modelId >= 0))
            {
                using var container = cache.ReadContainer(7, modelId);
                var part = ModelGeometryDecoder.Decode(container.Data.ToArray(), allowFirstTexelFallback: true);
                firstTexelFaces.AddRange((part.FirstTexelFaces ?? []).Select(face => face + colors.Count));
                var vertexOffset = positions.Count / 3;
                var faceOffset = colors.Count;
                if (placement is { } transform) PrepareObjectPart(part.Positions, part.Indices, part.FaceTextureUvs, transform, part.Version < 13 ? 4 : 1);
                emitters.AddRange((part.Emitters ?? []).Select(emitter => emitter with { Face = emitter.Face + faceOffset }));
                for (var coordinate = 0; coordinate < part.Positions.Length; coordinate++)
                {
                    var axis = coordinate % 3;
                    var axisScale = axis == 0 ? scaleX : axis == 1 ? scaleY : scaleZ;
                    var offset = axis == 0 ? offsetX : axis == 1 ? offsetY : offsetZ;
                    var unitScale = placement is null && part.Version < 13 ? 4 : 1;
                    positions.Add(((part.Positions[coordinate] * unitScale * axisScale) >> 7) + offset * 4);
                }
                indices.AddRange(part.Indices.Select(index => index + vertexOffset));
                colors.AddRange(part.FaceColors);
                alpha.AddRange(part.FaceAlpha);
                shadingTypes.AddRange(part.FaceShadingTypes ?? new int[part.FaceColors.Length]);
                priorities.AddRange(part.FacePriorities ?? new int[part.FaceColors.Length]);
                faceSkins.AddRange(part.FaceSkins ?? Enumerable.Repeat(-1, part.FaceColors.Length).ToArray());
                textureIds.AddRange(part.FaceTextureIds ?? Enumerable.Repeat(-1, part.FaceColors.Length).ToArray());
                textureVertices.AddRange((part.FaceTextureVertices ?? Enumerable.Repeat(-1, part.Indices.Length).ToArray()).Select(vertex => vertex < 0 ? -1 : vertex + vertexOffset));
                textureUvs.AddRange(part.FaceTextureUvs ?? new float[part.Indices.Length * 2]);
                skins.AddRange(part.VertexSkins ?? Enumerable.Repeat(-1, part.Positions.Length / 3).ToArray());
            }
            if (extraRotation)
                for (var vertex = 0; vertex < positions.Count; vertex += 3)
                {
                    var x = positions[vertex];
                    var z = positions[vertex + 2];
                    positions[vertex] = ((x + z) * 11585) >> 14;
                    positions[vertex + 2] = ((z - x) * 11585) >> 14;
                }
            ReplaceValues(colors, originalColors, replacementColors);
            ReplaceValues(textureIds, originalTextures, replacementTextures);
            for (var face = 0; face < textureIds.Count; face++)
                if (textureIds[face] == 65535) textureIds[face] = -1;
            return new GetModelDto(id, positions.ToArray(), indices.ToArray(), colors.ToArray(), alpha.ToArray(), skins.Any(skin => skin >= 0) ? skins.ToArray() : null,
                new[] { warning }.Concat(ProjectionWarnings(firstTexelFaces.ToArray())).ToArray(), animationIds.Where(animationId => animationId >= 0).Distinct().ToArray(), 1, faceSkins.ToArray(), textureIds.ToArray(), textureVertices.ToArray(), textureUvs.ToArray(), scale, ReadParticles(cache, emitters), priorities.ToArray(), shadingTypes.ToArray(), ambient, contrast, firstTexelFaces.ToArray());
        }
    }

    private static void PrepareObjectPart(int[] positions, int[] indices, float[]? uvs, (int Shape, int Orientation, bool Mirrored) placement, int unitScale)
    {
        for (var vertex = 0; vertex < positions.Length; vertex += 3)
        {
            var x = positions[vertex] * unitScale;
            var y = positions[vertex + 1] * unitScale;
            var z = positions[vertex + 2] * unitScale;
            if (placement.Mirrored) z = -z;
            if (placement.Shape == 4 && placement.Orientation > 3)
            {
                var originalX = x;
                x = (((x + z) * 11585) >> 14) + 180;
                z = (((z - originalX) * 11585) >> 14) - 180;
            }
            (x, z) = (placement.Orientation & 3) switch { 1 => (z, -x), 2 => (-x, -z), 3 => (-z, x), _ => (x, z) };
            positions[vertex] = x;
            positions[vertex + 1] = y;
            positions[vertex + 2] = z;
        }
        if (!placement.Mirrored) return;
        for (var face = 0; face < indices.Length; face += 3)
        {
            (indices[face + 1], indices[face + 2]) = (indices[face + 2], indices[face + 1]);
            if (uvs is not null)
                for (var axis = 0; axis < 2; axis++)
                    (uvs[face * 2 + 2 + axis], uvs[face * 2 + 4 + axis]) = (uvs[face * 2 + 4 + axis], uvs[face * 2 + 2 + axis]);
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

    // ICacheAPI.Read takes the member's physical position; archives may skip member IDs.
    private static System.IO.MemoryStream ReadMember(ICacheAPI cache, int indexId, int fileId, int memberId)
    {
        var member = cache.ReadReferenceTable(indexId).GetEntry(fileId, memberId)
            ?? throw new System.IO.FileNotFoundException($"Archive member {indexId}/{fileId}/{memberId} is absent from the cache reference table.");
        return cache.Read(indexId, fileId, member.Index);
    }

    private static ModelParticleDto[] ReadParticles(ICacheAPI cache, IEnumerable<ModelGeometryDecoder.EmitterAttachment> attachments)
    {
        var result = new List<ModelParticleDto>();
        foreach (var group in attachments.GroupBy(attachment => attachment.EmitterId))
        {
            ParticleEmitterDto? definition = null;
            string? warning = null;
            try
            {
                using var data = ReadMember(cache, 27, 0, group.Key);
                var decoded = ParticleEmitterDecoder.Decode(data.ToArray());
                definition = new ParticleEmitterDto(decoded.TextureId, decoded.ColorMin, decoded.ColorMax, decoded.LifeMin, decoded.LifeMax,
                    decoded.RateMin, decoded.RateMax, decoded.SizeMin, decoded.SizeMax, decoded.SpeedMin, decoded.SpeedMax, decoded.Angles, decoded.UnsupportedOpcodes);
            }
            catch (Exception exception) when (exception is System.IO.IOException or NotSupportedException)
            {
                warning = $"Particle emitter #{group.Key} is unavailable: {exception.Message}";
            }
            result.AddRange(group.Select(attachment => new ModelParticleDto(attachment.EmitterId, attachment.Face, definition, warning)));
        }
        return result.ToArray();
    }
}
