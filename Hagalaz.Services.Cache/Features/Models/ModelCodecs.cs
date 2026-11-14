using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Rendering;

namespace Hagalaz.Services.Cache.Features.Models;

/// <summary>The codecs model endpoints decode animation frames and rendering textures with, bound from DI with <c>[AsParameters]</c>.</summary>
internal sealed record ModelCodecs(
    ITypeCodec<IMaterialTableType> Materials,
    ISpriteTypeCodec Sprites,
    ITypeCodec<IAnimationSkeletonType> AnimationSkeletons,
    ITypeCodec<IAnimationFrameType> AnimationFrames)
{
    /// <summary>A texture source for one render.</summary>
    public TextureSource Textures(ICacheAPI cache) => new(cache, Materials, Sprites);
}
