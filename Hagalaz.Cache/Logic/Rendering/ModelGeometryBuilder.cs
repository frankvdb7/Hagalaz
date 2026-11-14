using System;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Logic.Rendering;

/// <summary>A model as the renderer uses it: resolved texture IDs, texture corners and coordinates, and attachments.</summary>
public sealed record ModelGeometry(int[] Positions, int[] Indices, int[] FaceColors, int[] FaceAlpha, int[]? VertexSkins, int Version, int[]? FaceSkins,
    int[]? FaceTextureIds, int[] FaceTextureVertices, float[]? FaceTextureUvs, ModelEmitterAttachment[]? Emitters, int[] FacePriorities,
    int[] FaceShadingTypes, int[]? FirstTexelFaces, ModelBillboardAttachment[]? Billboards, ModelEffectorAttachment[]? Effectors);

/// <summary>Derives render geometry from a stored model, as the revision-742 client does.</summary>
public static class ModelGeometryBuilder
{
    public static ModelGeometry Build(IModelType model)
    {
        var faces = model.FaceColors.Length;
        var textureVertices = new int[faces * 3];
        Array.Fill(textureVertices, -1);
        int[]? textureIds = null;
        float[]? textureUvs = null;
        int[]? firstTexelFaces = null;
        if (model.TextureIds is { } storedIds)
        {
            textureIds = storedIds.Select(id => id - 1).ToArray();
            var mappings = model.TextureMappings.Select(mapping => Mapping(mapping, model.Version)).ToArray();
            var pointers = new int[faces];
            Array.Fill(pointers, -1);
            for (var face = 0; face < faces; face++)
            {
                if (textureIds[face] < 0) continue;
                var mapping = mappings.Length > 0 ? model.MappingPointers![face] - 1 : -1;
                pointers[face] = mapping;
                if (mapping < 0) Array.Copy(model.Indices, face * 3, textureVertices, face * 3, 3);
                else if (mappings[mapping] is { Type: 0 } triangle)
                    (textureVertices[face * 3], textureVertices[face * 3 + 1], textureVertices[face * 3 + 2]) = (triangle.A, triangle.B, triangle.C);
            }
            // ModelDefinition.method1196 normalizes old positions and projection scales together.
            var projectionPositions = model.Version < 13 ? model.Positions.Select(coordinate => coordinate * 4).ToArray() : model.Positions;
            textureUvs = ModelTextureCoordinates.Generate(projectionPositions, model.Indices, textureIds, pointers, mappings, out firstTexelFaces);
        }

        return new ModelGeometry(
            model.Positions,
            model.Indices,
            model.FaceColors,
            model.FaceAlpha ?? new int[faces],
            model.VertexSkins?.Select(skin => skin == 255 ? -1 : skin).ToArray(),
            model.Version,
            model.FaceSkins,
            textureIds,
            textureVertices,
            textureUvs,
            model.Emitters?.ToArray(),
            model.FacePriorities ?? Enumerable.Repeat(model.GlobalPriority, faces).ToArray(),
            model.ShadingTypes ?? new int[faces],
            firstTexelFaces,
            model.Billboards?.Select(billboard => !model.UsesSmartBillboardGroups && billboard.Group == 255 ? billboard with { Group = -1 } : billboard).ToArray(),
            model.Effectors?.ToArray());
    }

    private static ModelTextureCoordinates.Mapping Mapping(ModelTextureMapping mapping, int version)
    {
        if (mapping.Type == 0) return new(0, mapping.A, mapping.B, mapping.C);
        var (sx, sy, sz) = (mapping.ScaleX, mapping.ScaleY, mapping.ScaleZ);
        if (version < 13) { sx *= 4; sy *= 4; if (mapping.Type != 1) sz *= 4; }
        return new(mapping.Type, (short)mapping.A, (short)mapping.B, (short)mapping.C, sx, sy, sz, mapping.Rotation, (sbyte)mapping.Direction,
            (sbyte)mapping.Offset, mapping.Type == 2 ? (sbyte)mapping.OffsetY : 0, mapping.Type == 2 ? (sbyte)mapping.OffsetZ : 0);
    }
}
