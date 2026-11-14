using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentResults;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Rendering;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Features.Types;
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
    private const string LightingDisclaimer = "Client model lighting uses a fixed inspection light; map atmosphere and seam lighting remain unverified.";
    private const string TransparencyDisclaimer = "Transparent faces use client priorities and depth ordering within each priority. Intersecting triangles and full client rendering parity remain unverified.";

    // Animation frames (index 0) and skeletons (index 1) are decoded here; model assembly does not read them.
    private const int AnimationFrameIndex = 0, AnimationSkeletonIndex = 1;

    public static IEndpointRouteBuilder MapModelEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/models/{id:int}", GetModel).WithSummary("Decode a model's geometry").Produces<GetModelDto>();
        group.MapGet("/models/{id:int}/image", GetModelImage).WithSummary("Render a model to a centred PNG preview").Produces<System.IO.Stream>(StatusCodes.Status200OK, "image/png");
        group.MapGet("/models/{id:int}/widget-image", GetWidgetModelImage).WithSummary("Render a model with a widget camera").Produces<System.IO.Stream>(StatusCodes.Status200OK, "image/png");
        group.MapGet("/models/capacity", (ICacheAPI cache) => TypedResults.Ok(new GetModelCapacityDto(cache.ReadReferenceTable(ModelAssembler.ModelIndex).Capacity))).WithSummary("Get the model index capacity");
        group.MapGet("/models/range", GetModelRange).WithSummary("List model IDs in a range of at most 200").Produces<GetModelSummaryDto[]>();
        group.MapGet("/types/items/{id:int}/model", GetItemModel).WithSummary("Assemble an item appearance model").Produces<GetModelDto>();
        group.MapGet("/types/items/{id:int}/model-appearances", GetItemAppearances).WithSummary("List an item's model appearances").Produces<GetItemModelAppearanceDto[]>();
        group.MapGet("/types/npcs/{id:int}/model", GetNpcModel).WithSummary("Assemble an NPC model").Produces<GetModelDto>();
        group.MapGet("/types/objects/{id:int}/model", GetObjectModel).WithSummary("Assemble an object model for one shape").Produces<GetModelDto>();
        group.MapGet("/types/objects/{id:int}/image", GetObjectImage).WithSummary("Render an object shape to a centred PNG preview").Produces<System.IO.Stream>(StatusCodes.Status200OK, "image/png");
        group.MapGet("/types/objects/{id:int}/render-appearance", GetObjectRenderAppearance).WithSummary("Assemble an object as placed in a map region").Produces<GetObjectRenderAppearanceDto>();
        group.MapPost("/types/objects/render-appearances", GetObjectRenderAppearances).WithSummary("Assemble up to 32 object placements, each with its own status").Produces<ObjectRenderAppearanceResultDto[]>();
        group.MapGet("/animation-frames/{id:int}", GetAnimationFrame).WithSummary("Decode an animation frame and its skeleton").Produces<GetAnimationFrameDto>();
        group.MapGet("/types/animations/{id:int}/sequence", GetAnimationSequence).WithSummary("Get an animation's frame sequence").Produces<GetAnimationSequenceDto>();
        return group;
    }

    private static IResult GetModel(int id, ModelAssembler assembler)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Model ID must be nonnegative."));
        return TypedResults.Ok(ToDto(assembler.Model(id), $"Geometry, projected textures and ordinary transparency preview. {TransparencyDisclaimer} {LightingDisclaimer}"));
    }

    private static IResult GetModelImage(int id, HttpContext context, ICacheAPI cache, ModelAssembler assembler, [AsParameters] ModelCodecs codecs, int width = 128, int height = 128)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Model ID must be nonnegative."));
        if (ValidateImageSize(width, height) is { } invalid) return invalid;
        var options = new ModelImages.Options(width, height);
        return RenderedImageResponse.Create(context, $"model-image-{ItemIcons.Validator(id, options, cache)}",
            () => ModelImages.RenderPng(assembler.ModelInClientUnits(id), options, codecs.Textures(cache)));
    }

    private static IResult GetWidgetModelImage(int id, HttpContext context, ICacheAPI cache, ModelAssembler assembler, [AsParameters] ModelCodecs codecs, [AsParameters] WidgetModelImageQuery query)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Model ID must be nonnegative."));
        if (ValidateImageSize(query.Width, query.Height) is { } invalid) return invalid;
        var options = new ModelImages.WidgetOptions(query.Width, query.Height,
            new ItemIconRenderer.WidgetCamera(query.Zoom, query.Pitch, query.Yaw, query.Roll, query.X, query.Y, query.Z,
                query.Flags & 15, query.BaseWidth, query.BaseHeight));
        return RenderedImageResponse.Create(context, $"widget-model-image-{ItemIcons.Validator(id, options, cache)}",
            () => ModelImages.RenderPng(assembler.ModelInClientUnits(id), new ModelImages.Options(options.Width, options.Height), codecs.Textures(cache), options.Camera));
    }

    internal sealed record WidgetModelImageQuery(int Width, int Height, ushort Zoom, ushort Pitch, ushort Yaw, ushort Roll,
        short X, short Y, short Z, byte Flags, ushort BaseWidth, ushort BaseHeight);

    private static IResult GetModelRange(int startId, int endIdExclusive, ICacheAPI cache)
    {
        if (startId < 0 || endIdExclusive < startId || (long)endIdExclusive - startId > 200)
            return TypedResults.BadRequest(EndpointErrors.Validation("Enter a model ID range of at most 200 IDs."));
        var table = cache.ReadReferenceTable(ModelAssembler.ModelIndex);
        return TypedResults.Ok(table.Entries.Select(entry => entry.Key).Where(id => id >= startId && id < endIdExclusive)
            .OrderBy(id => id).Select(id => new GetModelSummaryDto(id)).ToArray());
    }

    private static IResult GetAnimationSequence(int id, ITypeProvider<IAnimationType> animations)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Animation ID must be nonnegative."));
        var sequence = (AnimationType)animations.Get(id);
        return TypedResults.Ok(new GetAnimationSequenceDto(sequence.Id, sequence.Frames ?? [], sequence.Delays ?? [], sequence.Loop, sequence.IsTweened));
    }

    private static IResult GetAnimationFrame(int id, ICacheAPI cache, [AsParameters] ModelCodecs codecs)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Frame ID must be nonnegative."));
        var member = cache.ReadReferenceTable(AnimationFrameIndex).GetEntry(id >> 16, id & 65535)
            ?? throw new System.IO.FileNotFoundException($"Archive member {AnimationFrameIndex}/{id >> 16}/{id & 65535} is absent from the cache reference table.");
        using var stream = cache.Read(AnimationFrameIndex, id >> 16, member.Index);
        var stored = codecs.AnimationFrames.Decode(id, stream);
        using var skeletonContainer = cache.ReadContainer(AnimationSkeletonIndex, stored.SkeletonId);
        var skeleton = AnimationFrameBuilder.ResolveSkeleton(codecs.AnimationSkeletons.Decode(stored.SkeletonId, skeletonContainer.Data));
        var frame = AnimationFrameBuilder.Resolve(stored, skeleton);
        return TypedResults.Ok(new GetAnimationFrameDto(
            new AnimationFrameDto(frame.SkeletonId, frame.Transforms.Select(transform =>
                new AnimationTransformDto(transform.Slot, transform.X, transform.Y, transform.Z, transform.PivotSlot, transform.TweenFlags)).ToArray()),
            new AnimationSkeletonDto(skeleton.Types, skeleton.Enabled, skeleton.Masks, skeleton.Groups)));
    }

    private static IResult GetItemAppearances(int id, ITypeProvider<IItemType> items, ModelAssembler assembler)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Item ID must be nonnegative."));
        return TypedResults.Ok(assembler.ItemAppearances(items.Get(id)).Select(ToDto).ToArray());
    }

    private static IResult GetItemModel(int id, string? appearance, ModelAssembler assembler)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Item ID must be nonnegative."));
        var item = assembler.Item(id, appearance ?? ModelAssembler.InventoryAppearance);
        if (item.IsFailed) return EndpointErrors.ModelAssemblyFailure(item);
        return TypedResults.Ok(ToDto(item.Value.Model,
            $"{item.Value.Appearance.Label} model with definition recolors and retextures. Equipped/head models are isolated item parts, without a player body or customization. {TransparencyDisclaimer} {LightingDisclaimer}"));
    }

    private static IResult GetNpcModel(int id, ModelAssembler assembler)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("NPC ID must be nonnegative."));
        var npc = assembler.Npc(id);
        if (npc.IsFailed) return EndpointErrors.ModelAssemblyFailure(npc);
        return TypedResults.Ok(ToDto(npc.Value, $"Base NPC model preview with projected textures. Variant transforms, per-part offsets are not yet supported. {LightingDisclaimer}"));
    }

    private static IResult GetObjectModel(int id, int? shape, ModelAssembler assembler)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Object ID must be nonnegative."));
        var selection = assembler.SelectObjectShape(id, shape);
        if (selection.IsFailed) return EndpointErrors.ModelAssemblyFailure(selection);
        return TypedResults.Ok(ToDto(assembler.ObjectShape(selection.Value),
            $"Object shape {selection.Value.Shape} preview with projected textures. Placement rotation, contouring, variant transforms are not yet supported. {LightingDisclaimer}"));
    }

    private static IResult GetObjectImage(int id, HttpContext context, ICacheAPI cache, ModelAssembler assembler, [AsParameters] ModelCodecs codecs, int width = 128, int height = 128, int? shape = null)
    {
        if (id < 0) return TypedResults.BadRequest(EndpointErrors.Validation("Object ID must be nonnegative."));
        if (ValidateImageSize(width, height) is { } invalid) return invalid;
        var selection = assembler.SelectObjectShape(id, shape);
        if (selection.IsFailed) return EndpointErrors.ModelAssemblyFailure(selection);
        var tag = $"object-image-{ItemIcons.Validator(id, new ModelImages.ObjectOptions(width, height, shape), cache)}";
        return RenderedImageResponse.Create(context, tag,
            () => ModelImages.RenderPng(assembler.ObjectShapeInImageFrame(selection.Value), new ModelImages.Options(width, height), codecs.Textures(cache)));
    }

    private static IResult? ValidateImageSize(int width, int height) =>
        width is < 8 or > 512 || height is < 8 or > 512 ? TypedResults.BadRequest(EndpointErrors.Validation("width and height must be between 8 and 512.")) : null;

    private static IResult GetObjectRenderAppearances(ObjectRenderAppearanceKeyDto?[]? request, ModelAssembler assembler, CancellationToken cancellationToken)
    {
        if (request is null || request.Length is < 1 or > 32 || request.Any(key => key is null || key.Id < 0 || key.Shape is < 0 or > 22 || key.Rotation is < 0 or > 3))
            return TypedResults.BadRequest(EndpointErrors.Validation("Enter 1..32 appearance keys with nonnegative IDs, shapes 0..22 and rotations 0..3."));
        var rows = new List<ObjectRenderAppearanceResultDto>();
        foreach (var key in request.OfType<ObjectRenderAppearanceKeyDto>().Distinct())
        {
            cancellationToken.ThrowIfCancellationRequested();
            Result<ObjectPlacement> placement;
            try { placement = assembler.ObjectPlacement(key.Id, key.Shape, key.Rotation); }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                rows.Add(FailedRow(key, EndpointErrors.MapException(error)));
                continue;
            }
            rows.Add(placement.IsSuccess
                ? new ObjectRenderAppearanceResultDto(key.Id, key.Shape, key.Rotation, StatusCodes.Status200OK, ToDto(placement.Value), null)
                : FailedRow(key, EndpointErrors.ModelAssemblyFailure(placement)));
        }
        return TypedResults.Ok(rows.ToArray());
    }

    private static ObjectRenderAppearanceResultDto FailedRow(ObjectRenderAppearanceKeyDto key, IResult failure)
    {
        var status = (failure as IStatusCodeHttpResult)?.StatusCode ?? 500;
        var problem = (failure as ProblemHttpResult)?.ProblemDetails ?? (failure as IValueHttpResult)?.Value as ProblemDetails
            ?? new ProblemDetails { Status = status, Title = "Object appearance unavailable." };
        return new ObjectRenderAppearanceResultDto(key.Id, key.Shape, key.Rotation, status, null, problem);
    }

    private static IResult GetObjectRenderAppearance(int id, int shape, int rotation, ModelAssembler assembler)
    {
        if (id < 0 || shape is < 0 or > 22 || rotation is < 0 or > 3)
            return TypedResults.BadRequest(EndpointErrors.Validation("Enter a nonnegative object ID, shape 0..22 and rotation 0..3."));
        var placement = assembler.ObjectPlacement(id, shape, rotation);
        return placement.IsFailed ? EndpointErrors.ModelAssemblyFailure(placement) : TypedResults.Ok(ToDto(placement.Value));
    }

    private static GetObjectRenderAppearanceDto ToDto(ObjectPlacement placement) =>
        new(placement.Id, placement.Shape, placement.Rotation, placement.SizeX, placement.SizeY, placement.DecorDisplacement, placement.ContourMode,
            placement.ContourParameter, placement.Parts.Select(part => ToDto(part, "Region object appearance.")).ToArray(), placement.Warnings, placement.Name,
            placement.ShadowStrength, placement.ClipType, placement.Solid, placement.Gateway);

    private static GetItemModelAppearanceDto ToDto(ItemAppearance appearance) =>
        new(appearance.Key, appearance.Label, appearance.ModelIds, appearance.OffsetX, appearance.OffsetY, appearance.OffsetZ);

    /// <summary>Maps an assembled model, leading its warnings with the preview <paramref name="summary"/> and the model's notes.</summary>
    private static GetModelDto ToDto(AssembledModel model, string summary) =>
        new(model.Id, model.Positions, model.Indices, model.FaceColors, model.FaceAlpha, model.VertexSkins,
            [summary + string.Concat(model.Notes.Select(note => " " + note)), .. model.Warnings], model.AnimationIds, model.CoordinateScale, model.FaceSkins,
            model.FaceTextureIds, model.FaceTextureVertices, model.FaceTextureUvs, model.Scale, model.Particles.Select(ToDto).ToArray(), model.FacePriorities,
            model.FaceShadingTypes, model.Ambient, model.Contrast, model.FirstTexelFaces)
        {
            ObjectAnimation = model.ObjectAnimation is { } pose ? new ObjectAnimationPoseDto(pose.Rotation, pose.Offset, pose.PositionsBeforeExtraRotation) : null,
            Billboards = model.Billboards?.Select(ToDto).ToArray(),
            Effectors = model.Effectors?.Select(effector => new ModelEffectorDto(effector.EffectorId, effector.Vertex,
                effector.Definition is { } definition ? ToDto(definition) : null, effector.Warning)).ToArray()
        };

    private static ModelBillboardDto ToDto(ModelBillboard billboard) =>
        new(billboard.DefinitionId, billboard.Face, billboard.Group, billboard.DepthOffset,
            billboard.Definition is { } definition
                ? new ModelBillboardDefinitionDto(definition.RenderTextureId, definition.HalfWidth, definition.HalfHeight, definition.RenderBlendMode,
                    definition.HideFace, definition.HideInSpecialPass)
                : null,
            billboard.Warning);

    private static ModelParticleDto ToDto(ModelParticle particle) =>
        new(particle.EmitterId, particle.Face, particle.Definition is { } decoded
            ? new ParticleEmitterDto(decoded.RenderTextureId, decoded.ColorMin, decoded.ColorMax, decoded.LifeMin, decoded.LifeMax,
                decoded.RateMin, decoded.RateMax, decoded.SizeMin, decoded.SizeMax, decoded.SpeedMin, decoded.SpeedMax, decoded.RenderAngles, decoded.UnsupportedOpcodes,
                decoded.LocalEffectorIds, decoded.SpeedDrag is { } drag ? new ParticleDragDto(drag.Mode, drag.Strength) : null, decoded.ConstantEffectorIds,
                particle.ConstantEffectors.Select(ToDto).ToArray(),
                decoded.SizeOverLife is { } size ? new ParticleSizeCurveDto(size.Target, size.LifetimePercent) : null,
                decoded.SpeedOverLife is { } speed ? new ParticleSpeedCurveDto(speed.RawTarget, speed.LifetimePercent) : null,
                decoded.ColorOverLife is { } color ? new ParticleColorCurveDto(color.TargetArgb, color.RgbLifetimePercent, color.AlphaLifetimePercent) : null)
            : null, particle.Warning);

    private static ParticleEffectorDto ToDto(IParticleEffectorType effector)
    {
        var position = effector.Position ?? default;
        return new(position.X, position.Y, position.Z, effector.ConeAngle ?? 0, effector.Falloff?.Falloff ?? 0, effector.EffectiveAttenuation,
            effector.Scope ?? 0, effector.DirectDisplacement, effector.Radial, effector.Reverse);
    }
}
