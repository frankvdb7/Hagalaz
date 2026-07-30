using System;
using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Data.Model;

namespace Hagalaz.Services.GameWorld.Services.Cache;

public sealed class AnimationDefinitionCodec : ITypeCodec<IAnimationDefinition>
{
    private readonly AnimationTypeCodec _codec;

    public AnimationDefinitionCodec(AnimationTypeCodec codec) => _codec = codec;

    public IAnimationDefinition Decode(int id, MemoryStream stream)
    {
        var definition = new AnimationDefinition(id);
        _codec.Decode(definition, stream);
        return definition;
    }

    public MemoryStream Encode(IAnimationDefinition instance) => throw new NotSupportedException("Animation definitions are read-only cache data.");
}
