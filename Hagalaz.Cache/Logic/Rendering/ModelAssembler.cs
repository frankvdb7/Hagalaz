using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentResults;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Rendering;

/// <summary>The definition has nothing to assemble: no shapes, no models for the shape, or no available appearance.</summary>
public sealed class ModelSubjectAbsentError() : Error("The definition has no model to assemble.");

/// <summary>The requested item appearance key is not one the client defines.</summary>
public sealed class UnknownAppearanceError(string key) : Error($"Unknown item model appearance '{key}'.");

/// <summary>The definition uses an appearance the assembler does not reproduce yet.</summary>
public sealed class UnsupportedAppearanceError(string detail) : Error(detail);

/// <summary>
/// One model assembled as the client does: parts merged, scaled, offset, recoloured and retextured, with attachments resolved.
/// <see cref="Warnings"/> are separate findings from the data; <see cref="Notes"/> are sentences that qualify the preview as a whole.
/// </summary>
public sealed record AssembledModel(int Id, int[] Positions, int[] Indices, int[] FaceColors, int[] FaceAlpha, int[]? VertexSkins,
    string[] Warnings, string[] Notes, int[]? AnimationIds, float CoordinateScale, int[]? FaceSkins, int[]? FaceTextureIds,
    int[]? FaceTextureVertices, float[]? FaceTextureUvs, float[]? Scale, ModelParticle[] Particles, int[]? FacePriorities,
    int[]? FaceShadingTypes, int Ambient, int Contrast, int[]? FirstTexelFaces, ObjectAnimationPose? ObjectAnimation,
    ModelBillboard[]? Billboards, ModelEffector[]? Effectors);

/// <summary>A particle emitter attached to a face; <see cref="Definition"/> is null when it could not be read.</summary>
public sealed record ModelParticle(int EmitterId, int Face, IParticleEmitterType? Definition, IParticleEffectorType[] ConstantEffectors, string? Warning);

/// <summary>A billboard attached to a face; <see cref="Definition"/> is null when it could not be read.</summary>
public sealed record ModelBillboard(int DefinitionId, int Face, int Group, int DepthOffset, IBillboardType? Definition, string? Warning);

/// <summary>A particle effector attached to a vertex; <see cref="Definition"/> is null when it could not be read.</summary>
public sealed record ModelEffector(int EffectorId, int Vertex, IParticleEffectorType? Definition, string? Warning);

/// <summary>The pose an animated placed object needs to re-apply its placement after skinning.</summary>
public sealed record ObjectAnimationPose(int Rotation, int[] Offset, int[]? PositionsBeforeExtraRotation);

/// <summary>An item appearance the client can show, with its model parts and wear offsets.</summary>
public sealed record ItemAppearance(string Key, string Label, int[] ModelIds, int OffsetX = 0, int OffsetY = 0, int OffsetZ = 0)
{
    public bool Available => ModelIds.Length > 0;
}

/// <summary>An assembled item appearance.</summary>
public sealed record ItemModel(ItemAppearance Appearance, AssembledModel Model);

/// <summary>An object shape whose models exist, selected without assembling it.</summary>
public sealed record ObjectShapeSelection(int ObjectId, int Shape, int[] ModelIds)
{
    internal ObjectType Definition { get; init; } = null!;
}

/// <summary>An object as placed in a map region, with the placed definition's base, shadow and collision fields.</summary>
public sealed record ObjectPlacement(int Id, int Shape, int Rotation, int SizeX, int SizeY, int DecorDisplacement, int ContourMode,
    int ContourParameter, AssembledModel[] Parts, string[] Warnings, string? Name, int ShadowStrength, int ClipType, bool Solid, bool Gateway);

/// <summary>Assembles item, NPC, object and bare models the way the revision-742 client does.</summary>
public sealed class ModelAssembler(
    ICacheAPI cache,
    ITypeProvider<IItemType> items,
    ITypeProvider<INpcType> npcs,
    ITypeProvider<IObjectType> objects,
    ITypeCodec<IModelType> models,
    ITypeCodec<IParticleEmitterType> emitters,
    ITypeCodec<IParticleEffectorType> effectors,
    ITypeCodec<IBillboardType> billboards,
    ITypeCodec<IRenderAnimationType> renderAnimations)
{
    /// <summary>The index holding models, one container per model ID.</summary>
    public const int ModelIndex = 7;

    /// <summary>The config index and archive holding render animation sets.</summary>
    internal const int RenderAnimationIndex = 2, RenderAnimationArchive = 32;

    /// <summary>The index holding particle emitters (archive 0) and effectors (archive 1).</summary>
    internal const int ParticleIndex = 27, EmitterArchive = 0, EffectorArchive = 1;

    /// <summary>The index and archive holding billboards.</summary>
    internal const int BillboardIndex = 29, BillboardArchive = 0;

    /// <summary>The appearance key of an item's inventory model.</summary>
    public const string InventoryAppearance = "inventory";

    /// <summary>A bare model as stored: positions in stored units with their coordinate scale, and resolved attachments.</summary>
    public AssembledModel Model(int modelId)
    {
        using var container = cache.ReadContainer(ModelIndex, modelId);
        var geometry = ModelGeometryBuilder.Build(models.Decode(modelId, container.Data));
        return new AssembledModel(modelId, geometry.Positions, geometry.Indices, geometry.FaceColors, geometry.FaceAlpha, geometry.VertexSkins,
            ProjectionWarnings(geometry.FirstTexelFaces), [], null, geometry.Version < 13 ? 0.25f : 1, geometry.FaceSkins, geometry.FaceTextureIds,
            geometry.FaceTextureVertices, geometry.FaceTextureUvs, null, ReadParticles(geometry.Emitters ?? []), geometry.FacePriorities,
            geometry.FaceShadingTypes, 64, 850, geometry.FirstTexelFaces, null, ReadBillboards(geometry.Billboards ?? []), ReadEffectors(geometry.Effectors ?? []));
    }

    /// <summary>A bare model in client units at scale 128, as model images frame it.</summary>
    public AssembledModel ModelInClientUnits(int modelId) =>
        Assemble(modelId, [modelId], 128, 128, 128, 0, 0, 0, null, null, null, null, []);

    /// <summary>The appearances an item defines, in client order.</summary>
    public IReadOnlyList<ItemAppearance> ItemAppearances(IItemType item) =>
    [
        new(InventoryAppearance, "Inventory", Parts(item.InterfaceModelId)),
        new("male-equipped", "Male equipped", Parts(item.MaleWornModelId1, item.MaleWornModelId2, item.MaleWornModelId3), item.MaleWearXOffset, item.MaleWearYOffset, item.MaleWearZOffset),
        new("female-equipped", "Female equipped", Parts(item.FemaleWornModelId1, item.FemaleWornModelId2, item.FemaleWornModelId3), item.FemaleWearXOffset, item.FemaleWearYOffset, item.FemaleWearZOffset),
        new("male-head", "Male head", Parts(item.MaleHeadModel, item.MaleHeadModel2)),
        new("female-head", "Female head", Parts(item.FemaleHeadModel, item.FemaleHeadModel2)),
    ];

    /// <summary>Assembles item <paramref name="itemId"/>'s appearance <paramref name="appearanceKey"/>.</summary>
    public Result<ItemModel> Item(int itemId, string appearanceKey) => Item(items.Get(itemId), appearanceKey);

    /// <summary>Assembles <paramref name="item"/>'s appearance <paramref name="appearanceKey"/> with definition scale, recolours and retextures.</summary>
    public Result<ItemModel> Item(IItemType item, string appearanceKey)
    {
        var selected = ItemAppearances(item).FirstOrDefault(value => value.Key == appearanceKey);
        if (selected is null) return new UnknownAppearanceError(appearanceKey);
        if (!selected.Available) return new ModelSubjectAbsentError();
        var definition = (ItemType)item;
        var inventory = selected.Key == InventoryAppearance;
        // Item offsets have already been shifted by two in the definition codec; assembly offsets use unshifted units.
        var model = Assemble(selected.ModelIds[0], selected.ModelIds, inventory ? definition.ScaleX : 128, inventory ? definition.ScaleY : 128, inventory ? definition.ScaleZ : 128,
            selected.OffsetX / 4, selected.OffsetY / 4, selected.OffsetZ / 4,
            Pairs(definition.OriginalModelColors), Pairs(definition.ModifiedModelColors), Pairs(definition.OriginalTextureColors), Pairs(definition.ModifiedTextureColors), [],
            ambient: 64 + definition.Ambient, contrast: (inventory ? 768 : 850) + definition.Contrast,
            notes: definition.UnknownArray1 is { Length: > 0 } ? ["Palette-index recolor overrides are not yet supported."] : []);
        return new ItemModel(selected, model);

        static short[]? Pairs(int[]? values) => values?.Select(value => (short)value).ToArray();
    }

    /// <summary>Assembles NPC <paramref name="npcId"/>'s base model with its animation associations.</summary>
    public Result<AssembledModel> Npc(int npcId)
    {
        var npc = (NpcType)npcs.Get(npcId);
        if (npc.ModelIDs is not { Length: > 0 }) return new ModelSubjectAbsentError();
        var animationIds = new List<int> { npc.IdleAnimationId, npc.MoveType1AnimationId, npc.MoveType2AnimationId, npc.RunAnimationId };
        string[] notes = [];
        if (npc.RenderId >= 0)
        {
            try
            {
                using var renderSet = ReadMember(RenderAnimationIndex, RenderAnimationArchive, npc.RenderId);
                animationIds.AddRange(renderAnimations.Decode(npc.RenderId, renderSet).SequenceIds);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or NotSupportedException)
            {
                notes = ["Render-animation associations could not be decoded: " + exception.Message];
            }
        }
        return Assemble(npcId, npc.ModelIDs, 128, 128, 128, 0, 0, 0,
            npc.OriginalColours, npc.ModifiedColours, npc.AShortArray1508, npc.AShortArray1491, animationIds.ToArray(),
            [npc.ScaleX / 128f, npc.ScaleY / 128f, npc.ScaleX / 128f], 64 + npc.LightModifier, 850 + npc.ShadowModifier, notes: notes);
    }

    /// <summary>Selects object <paramref name="objectId"/>'s <paramref name="shape"/> (its first shape when null) without assembling it.</summary>
    public Result<ObjectShapeSelection> SelectObjectShape(int objectId, int? shape)
    {
        var definition = (ObjectType)objects.Get(objectId);
        if (definition.Shapes is not { Length: > 0 }) return new ModelSubjectAbsentError();
        var selectedShape = shape ?? definition.Shapes[0];
        var group = Array.FindIndex(definition.Shapes, value => value == selectedShape);
        if (group < 0 || group >= definition.ModelIDs.Length) return new ModelSubjectAbsentError();
        if (definition.Inverted) return new UnsupportedAppearanceError("Inverted object models are not yet supported.");
        return new ObjectShapeSelection(objectId, selectedShape, definition.ModelIDs[group]) { Definition = definition };
    }

    /// <summary>Assembles a selected object shape with the definition's scale, offsets, recolours, retextures and animations.</summary>
    public AssembledModel ObjectShape(ObjectShapeSelection selection)
    {
        var definition = selection.Definition;
        return Assemble(selection.ObjectId, selection.ModelIds, definition.ScaleX, definition.ScaleY, definition.ScaleZ,
            definition.OffsetX, definition.OffsetY, definition.OffsetZ, definition.OriginalColors, definition.ModifiedColors, definition.AShortArray831, definition.AShortArray762, definition.AnimationIDs ?? [],
            ambient: 64 + definition.Ambient, contrast: 850 + definition.Contrast);
    }

    /// <summary>Assembles a selected object shape as object images frame it: definition scale and colours, no offsets or animations.</summary>
    public AssembledModel ObjectShapeInImageFrame(ObjectShapeSelection selection)
    {
        var definition = selection.Definition;
        return Assemble(selection.ObjectId, selection.ModelIds, definition.ScaleX, definition.ScaleY, definition.ScaleZ, 0, 0, 0,
            definition.OriginalColors, definition.ModifiedColors, definition.AShortArray831, definition.AShortArray762, [],
            ambient: 64 + definition.Ambient, contrast: 850 + definition.Contrast);
    }

    /// <summary>Assembles object <paramref name="objectId"/> placed with <paramref name="shape"/> and <paramref name="rotation"/>, at zero varp/varbit state.</summary>
    public Result<ObjectPlacement> ObjectPlacement(int objectId, int shape, int rotation)
    {
        var definition = (ObjectType)objects.Get(objectId);
        var name = definition.Name;
        // The client reads these from the placed definition, before any transform variant is chosen.
        var castsShadow = definition.CastsShadow;
        var (clipType, solid, gateway) = (definition.ClipType, definition.Solid, definition.Gateway);
        var isStatic = definition is { AnimationIDs: null, HasAnimation: false, TransformToIDs: null, ABoolean835: false };
        var modelShape = shape is >= 4 and <= 8 ? 4 : shape == 11 ? 10 : shape;
        var warnings = new List<string>();
        if (definition.TransformToIDs is { Length: > 0 } variants)
        {
            // Match ObjectDefinition.getTransformToDefinition with zero-valued static preview state.
            var hasSelector = definition.VarpBitFileId >= 0 || definition.VarpFileId >= 0;
            var selected = hasSelector && variants.Length > 1 ? variants[0] : variants[^1];
            var state = hasSelector ? "assumed zero-valued varp/varbit state" : "the definition's default transform fallback";
            if (selected == -1)
                return new ObjectPlacement(objectId, shape, rotation, definition.SizeX, definition.SizeY, definition.DecorDisplacement,
                    definition.GroundContoured, definition.AnInt780, [], [$"Hidden for {state}; live game state is unavailable."], name, ShadowStrength(castsShadow, isStatic, null), clipType, solid, gateway);
            definition = (ObjectType)objects.Get(selected);
            warnings.Add($"Preview uses object {selected} for {state}; live game state is unavailable.");
        }
        var group = Array.FindIndex(definition.Shapes ?? [], value => value == modelShape);
        if (group < 0 || group >= definition.ModelIDs.Length) return new ModelSubjectAbsentError();
        if (definition.AByteArray816 is { Length: > 0 }) warnings.Add("Palette-index recolor overrides are unavailable.");
        if (definition.AByte787 != 0) warnings.Add("The definition's additional color adjustment is unavailable.");
        if (definition.GroundContoured > 2) warnings.Add($"Terrain contouring mode {definition.GroundContoured} is unavailable.");
        var orientations = shape == 2 ? new[] { rotation + 4, (rotation + 1) & 3 }
            : shape == 8 ? new[] { rotation + 4, ((rotation + 2) & 3) + 4 }
            : new[] { shape == 6 ? rotation + 4 : shape == 7 ? ((rotation + 2) & 3) + 4 : rotation };
        var parts = new List<AssembledModel>();
        foreach (var orientation in orientations)
        {
            var mirrored = definition.Inverted || (modelShape == 2 && orientation > 3);
            var model = Assemble(objectId, definition.ModelIDs[group], definition.ScaleX, definition.ScaleY, definition.ScaleZ,
                definition.OffsetX / 4, definition.OffsetY / 4, definition.OffsetZ / 4,
                definition.OriginalColors, definition.ModifiedColors, definition.AShortArray831, definition.AShortArray762, definition.AnimationIDs ?? [],
                ambient: 64 + definition.Ambient, contrast: 850 + definition.Contrast,
                placement: (modelShape, orientation, mirrored), extraRotation: shape == 11);
            parts.Add(model);
            warnings.AddRange(model.Warnings);
        }
        return new ObjectPlacement(objectId, shape, rotation, definition.SizeX, definition.SizeY, definition.DecorDisplacement,
            definition.GroundContoured, definition.AnInt780, parts.ToArray(), warnings.ToArray(), name, ShadowStrength(castsShadow, isStatic, parts[0]), clipType, solid, gateway);
    }

    // The client ignores secondary/tertiary references after the first absent part.
    private static int[] Parts(params int[] ids) => ids.TakeWhile(id => id >= 0).ToArray();

    private static string[] ProjectionWarnings(int[]? faces) => faces is { Length: > 0 }
        ? [$"First-texel texture fallback on {faces.Length} faces with invalid projection coordinates."] : [];

    // Map.addToClip: a ground object's floor shadow is its horizontal radius over 4, capped at 30, or 15 when it can animate or transform.
    // Walls use a fixed strength, so any object that casts a shadow reports at least 1.
    private static int ShadowStrength(bool castsShadow, bool isStatic, AssembledModel? model)
    {
        if (!castsShadow) return 0;
        if (!isStatic || model is null) return 15;
        long furthest = 0;
        for (var vertex = 0; vertex < model.Positions.Length; vertex += 3)
            furthest = Math.Max(furthest, (long)model.Positions[vertex] * model.Positions[vertex] + (long)model.Positions[vertex + 2] * model.Positions[vertex + 2]);
        return Math.Max(1, Math.Min(30, (int)(Math.Sqrt(furthest) + 0.99) / 4));
    }

    private AssembledModel Assemble(int id, int[] modelIds, int scaleX, int scaleY, int scaleZ,
        int offsetX, int offsetY, int offsetZ, short[]? originalColors, short[]? replacementColors, short[]? originalTextures, short[]? replacementTextures, int[] animationIds,
        float[]? scale = null, int ambient = 64, int contrast = 850, (int Shape, int Orientation, bool Mirrored)? placement = null, bool extraRotation = false, string[]? notes = null)
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
        var emitterAttachments = new List<ModelEmitterAttachment>();
        var effectorAttachments = new List<ModelEffectorAttachment>();
        var billboardAttachments = new List<ModelBillboardAttachment>();
        foreach (var modelId in modelIds.Where(modelId => modelId >= 0))
        {
            using var container = cache.ReadContainer(ModelIndex, modelId);
            var part = ModelGeometryBuilder.Build(models.Decode(modelId, container.Data));
            firstTexelFaces.AddRange((part.FirstTexelFaces ?? []).Select(face => face + colors.Count));
            var vertexOffset = positions.Count / 3;
            var faceOffset = colors.Count;
            if (placement is { } transform) PrepareObjectPart(part.Positions, part.Indices, part.FaceTextureUvs, transform, part.Version < 13 ? 4 : 1);
            emitterAttachments.AddRange((part.Emitters ?? []).Select(emitter => emitter with { Face = emitter.Face + faceOffset }));
            effectorAttachments.AddRange((part.Effectors ?? []).Select(effector => effector with { Vertex = effector.Vertex + vertexOffset }));
            billboardAttachments.AddRange((part.Billboards ?? []).Select(billboard => billboard with { Face = billboard.Face + faceOffset }));
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
        var objectAnimation = placement is { } placed && animationIds.Any(animationId => animationId >= 0)
            && (skins.Any(skin => skin >= 0) || faceSkins.Any(skin => skin >= 0) || billboardAttachments.Any(billboard => billboard.Group >= 0))
            ? new ObjectAnimationPose(placed.Orientation & 3, [offsetX * 4, offsetY * 4, offsetZ * 4], extraRotation ? positions.ToArray() : null)
            : null;
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
        var firstTexel = firstTexelFaces.ToArray();
        return new AssembledModel(id, positions.ToArray(), indices.ToArray(), colors.ToArray(), alpha.ToArray(), skins.Any(skin => skin >= 0) ? skins.ToArray() : null,
            ProjectionWarnings(firstTexel), notes ?? [], animationIds.Where(animationId => animationId >= 0).Distinct().ToArray(), 1, faceSkins.ToArray(), textureIds.ToArray(),
            textureVertices.ToArray(), textureUvs.ToArray(), scale, ReadParticles(emitterAttachments), priorities.ToArray(), shadingTypes.ToArray(), ambient, contrast, firstTexel,
            objectAnimation, ReadBillboards(billboardAttachments), ReadEffectors(effectorAttachments));
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
    private MemoryStream ReadMember(int indexId, int fileId, int memberId)
    {
        var member = cache.ReadReferenceTable(indexId).GetEntry(fileId, memberId)
            ?? throw new FileNotFoundException($"Archive member {indexId}/{fileId}/{memberId} is absent from the cache reference table.");
        return cache.Read(indexId, fileId, member.Index);
    }

    private ModelBillboard[]? ReadBillboards(IReadOnlyList<ModelBillboardAttachment> attachments)
    {
        if (attachments.Count == 0) return null;
        var definitions = new Dictionary<int, (IBillboardType? Definition, string? Warning)>();
        var result = new ModelBillboard[attachments.Count];
        for (var i = 0; i < result.Length; i++)
        {
            var attachment = attachments[i];
            if (!definitions.TryGetValue(attachment.DefinitionId, out var resolved))
            {
                try
                {
                    using var data = ReadMember(BillboardIndex, BillboardArchive, attachment.DefinitionId);
                    resolved = (billboards.Decode(attachment.DefinitionId, data), null);
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException)
                {
                    resolved = (null, $"Model billboard #{attachment.DefinitionId} is unavailable: {exception.Message}");
                }
                definitions.Add(attachment.DefinitionId, resolved);
            }
            result[i] = new(attachment.DefinitionId, attachment.Face, attachment.Group, attachment.DepthOffset, resolved.Definition, resolved.Warning);
        }
        return result;
    }

    private ModelParticle[] ReadParticles(IEnumerable<ModelEmitterAttachment> attachments)
    {
        var result = new List<ModelParticle>();
        foreach (var group in attachments.GroupBy(attachment => attachment.EmitterId))
        {
            IParticleEmitterType? definition = null;
            IParticleEffectorType[] constants = [];
            string? warning = null;
            try
            {
                using var data = ReadMember(ParticleIndex, EmitterArchive, group.Key);
                var decoded = emitters.Decode(group.Key, data);
                constants = ReadConstantEffectors(decoded.ConstantEffectorIds, out warning);
                definition = decoded;
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or NotSupportedException)
            {
                warning = $"Particle emitter #{group.Key} is unavailable: {exception.Message}";
            }
            result.AddRange(group.Select(attachment => new ModelParticle(attachment.EmitterId, attachment.Face, definition, constants, warning)));
        }
        return result.ToArray();
    }

    private ModelEffector[]? ReadEffectors(IReadOnlyList<ModelEffectorAttachment> attachments)
    {
        if (attachments.Count == 0) return null;
        var definitions = attachments.Select(attachment => attachment.EffectorId).Distinct()
            .ToDictionary(id => id, id => ReadEffector(id));
        return attachments.Select(attachment =>
        {
            var (definition, warning) = definitions[attachment.EffectorId];
            return new ModelEffector(attachment.EffectorId, attachment.Vertex, definition, warning);
        }).ToArray();
    }

    private IParticleEffectorType[] ReadConstantEffectors(int[] ids, out string? warning)
    {
        var result = new List<IParticleEffectorType>();
        var warnings = new List<string>();
        foreach (var id in ids)
        {
            var (definition, unavailable) = ReadEffector(id, expectedScope: 2);
            if (unavailable is not null) warnings.Add(unavailable);
            else if (definition is not null) result.Add(definition);
        }
        warning = warnings.Count == 0 ? null : string.Join(" ", warnings);
        return result.ToArray();
    }

    private (IParticleEffectorType? Definition, string? Warning) ReadEffector(int id, int expectedScope = 0)
    {
        try
        {
            using var data = ReadMember(ParticleIndex, EffectorArchive, id);
            var definition = effectors.Decode(id, data);
            var scope = definition.Scope ?? 0;
            var warning = scope == expectedScope ? null : $"Particle effector #{id} uses an unsupported scope ({scope}).";
            return (definition, warning);
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException)
        {
            return (null, $"Particle effector #{id} is unavailable: {exception.Message}");
        }
    }
}
