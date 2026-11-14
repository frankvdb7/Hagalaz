using System.Collections.Generic;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types;

public sealed class ModelType(int id) : IModelType
{
    public int Id { get; } = id;
    public int Version { get; set; } = 12;
    public int GlobalPriority { get; set; }
    public bool UsesSmartBillboardGroups { get; set; }
    public int[] Positions { get; set; } = [];
    public int[] Indices { get; set; } = [];
    public byte[] StripCodes { get; set; } = [];
    public int[] FaceColors { get; set; } = [];
    public int[]? FacePriorities { get; set; }
    public int[]? FaceAlpha { get; set; }
    public int[]? FaceSkins { get; set; }
    public int[]? VertexSkins { get; set; }
    public int[]? ShadingTypes { get; set; }
    public int[]? TextureIds { get; set; }
    public int[]? MappingPointers { get; set; }
    public IList<ModelTextureMapping> TextureMappings { get; } = new List<ModelTextureMapping>();
    public IList<ModelEmitterAttachment>? Emitters { get; set; }
    public IList<ModelEffectorAttachment>? Effectors { get; set; }
    public IList<ModelBillboardAttachment>? Billboards { get; set; }
}
