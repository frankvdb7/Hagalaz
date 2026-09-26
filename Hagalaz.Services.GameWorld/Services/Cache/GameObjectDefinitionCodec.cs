using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Services.GameWorld.Data.Model;

namespace Hagalaz.Services.GameWorld.Services.Cache;

public sealed class GameObjectDefinitionCodec : ITypeCodec<GameObjectDefinition>
{
    private readonly ObjectTypeCodec _codec;

    public GameObjectDefinitionCodec(ObjectTypeCodec codec) => _codec = codec;

    public GameObjectDefinition Decode(int id, MemoryStream stream)
    {
        var definition = new GameObjectDefinition(id);
        _codec.Decode(definition, stream);
        return definition;
    }

    public MemoryStream Encode(GameObjectDefinition instance) => _codec.Encode(instance);
}
