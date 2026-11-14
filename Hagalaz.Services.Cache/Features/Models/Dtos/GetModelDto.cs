using System.Text.Json.Serialization;

namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record GetModelDto(int Id, int[] Positions, int[] Indices, int[] FaceColors, int[] FaceAlpha, int[]? VertexSkins, string[] Warnings, int[]? AnimationIds = null, float CoordinateScale = 1, int[]? FaceSkins = null, int[]? FaceTextureIds = null, int[]? FaceTextureVertices = null, float[]? FaceTextureUvs = null, float[]? Scale = null, ModelParticleDto[]? Particles = null, int[]? FacePriorities = null, int[]? FaceShadingTypes = null, int Ambient = 64, int Contrast = 850, int[]? FirstTexelFaces = null)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ObjectAnimationPoseDto? ObjectAnimation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ModelBillboardDto[]? Billboards { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ModelEffectorDto[]? Effectors { get; init; }
}

public sealed record ObjectAnimationPoseDto(int Rotation, int[] Offset, int[]? PositionsBeforeExtraRotation);

public sealed record ModelBillboardDto(int DefinitionId, int Face, int Group, int DepthOffset, ModelBillboardDefinitionDto? Definition, string? Warning);

public sealed record ModelBillboardDefinitionDto(int TextureId, int HalfWidth, int HalfHeight, int BlendMode, bool HideFace, bool HideInSpecialPass);

public sealed record ModelEffectorDto(int EffectorId, int Vertex, ParticleEffectorDto? Definition, string? Warning);

public sealed record ParticleEffectorDto(int X, int Y, int Z, int ConeAngle, int Falloff, int Attenuation, int Scope,
    bool DirectDisplacement, bool Radial, bool Reverse);
