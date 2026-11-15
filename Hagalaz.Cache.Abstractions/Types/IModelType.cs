using System.Collections.Generic;

namespace Hagalaz.Cache.Abstractions.Types;

/// <summary>
/// A model (cache index 7, newer layout). Values are as stored: positions are absolute but unscaled, texture IDs and mapping
/// pointers keep their stored offset of one (0 meaning none), and optional per-face or per-vertex streams are <c>null</c> when
/// their switch is off.
/// </summary>
public interface IModelType : IType
{
    /// <summary>12 (no version byte), 14 or 15; decides the projection scale widths.</summary>
    int Version { get; set; }
    /// <summary>The priority of every face, or 255 when <see cref="FacePriorities"/> holds one per face.</summary>
    int GlobalPriority { get; set; }
    /// <summary>Flag 0x40: billboard groups stored as two-byte smart values instead of bytes.</summary>
    bool UsesSmartBillboardGroups { get; set; }

    /// <summary>Absolute x, y, z per vertex.</summary>
    int[] Positions { get; set; }
    /// <summary>Three vertex indices per face.</summary>
    int[] Indices { get; set; }
    /// <summary>The stored strip code (1-4) per face, which decides how its indices are encoded.</summary>
    byte[] StripCodes { get; set; }
    /// <summary>Raw colour per face; on textured faces the stored colour is kept even though the texture is drawn.</summary>
    int[] FaceColors { get; set; }
    int[]? FacePriorities { get; set; }
    int[]? FaceAlpha { get; set; }
    int[]? FaceSkins { get; set; }
    /// <summary>Raw skin group per vertex, 255 meaning none.</summary>
    int[]? VertexSkins { get; set; }
    /// <summary>Flag 0x01: the stored shading type per face.</summary>
    int[]? ShadingTypes { get; set; }
    /// <summary>Texture ID plus one per face, 0 meaning untextured.</summary>
    int[]? TextureIds { get; set; }
    /// <summary>Mapping index plus one per textured face (0 meaning the face's own corners); ignored on untextured faces.</summary>
    int[]? MappingPointers { get; set; }
    IList<ModelTextureMapping> TextureMappings { get; }

    /// <summary>Flag 0x02 (with <see cref="Effectors"/>): particle emitters.</summary>
    IList<ModelEmitterAttachment>? Emitters { get; set; }
    IList<ModelEffectorAttachment>? Effectors { get; set; }
    /// <summary>Flag 0x04: billboards, with groups as stored.</summary>
    IList<ModelBillboardAttachment>? Billboards { get; set; }
}

/// <summary>
/// A texture mapping as stored: type 0 is a triangle of vertices; types 1-3 are projections with raw scales, rotation,
/// direction and offsets (the box offsets only for type 2). Signed values are kept as their stored unsigned bits.
/// </summary>
public readonly record struct ModelTextureMapping(int Type, int A, int B, int C, int ScaleX = 0, int ScaleY = 0, int ScaleZ = 0,
    int Rotation = 0, int Direction = 0, int Offset = 0, int OffsetY = 0, int OffsetZ = 0);

public readonly record struct ModelEmitterAttachment(int EmitterId, int Face);

public readonly record struct ModelEffectorAttachment(int EffectorId, int Vertex);

/// <summary>A billboard attachment; in a decoded model its group is as stored, in built geometry 255 byte groups become -1.</summary>
public readonly record struct ModelBillboardAttachment(int DefinitionId, int Face, int Group, int DepthOffset);
