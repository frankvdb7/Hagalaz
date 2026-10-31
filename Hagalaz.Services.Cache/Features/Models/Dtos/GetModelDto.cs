namespace Hagalaz.Services.Cache.Features.Models.Dtos;

public sealed record GetModelDto(int Id, int[] Positions, int[] Indices, int[] FaceColors, int[] FaceAlpha, int[]? VertexSkins, string[] Warnings, int[]? AnimationIds = null, float CoordinateScale = 1, int[]? FaceSkins = null, int[]? FaceTextureIds = null, int[]? FaceTextureVertices = null, float[]? FaceTextureUvs = null, float[]? Scale = null, ModelParticleDto[]? Particles = null);
