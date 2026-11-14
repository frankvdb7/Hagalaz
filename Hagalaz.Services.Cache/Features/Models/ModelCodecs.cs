using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Services.Cache.Features.Models;

/// <summary>The codecs model assembly decodes attached definitions with, bound from DI with <c>[AsParameters]</c>.</summary>
internal sealed record ModelCodecs(
    ITypeCodec<IParticleEmitterType> Emitters,
    ITypeCodec<IParticleEffectorType> Effectors,
    ITypeCodec<IBillboardType> Billboards,
    ITypeCodec<IRenderAnimationType> RenderAnimations,
    ITypeCodec<IMaterialTableType> Materials,
    ITypeCodec<IModelType> Models);
