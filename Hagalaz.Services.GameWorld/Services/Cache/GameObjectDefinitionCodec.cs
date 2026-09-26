using System.IO;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Diagnostics;

namespace Hagalaz.Services.GameWorld.Services.Cache;

public sealed class GameObjectDefinitionCodec : ITypeCodec<GameObjectDefinition>
{
    private readonly ObjectTypeCodec _codec;

    public GameObjectDefinitionCodec(ObjectTypeCodec codec) => _codec = codec;

    public GameObjectDefinition Decode(int id, MemoryStream stream)
    {
        var definition = new GameObjectDefinition(id);
        var activity = GameObjectDefinitionResolutionDiagnostics.CurrentCompositionActivity;
        var decodeStart = GameObjectDefinitionResolutionDiagnostics.StartTiming(activity);
        try
        {
            _codec.Decode(definition, stream);
            GameObjectDefinitionResolutionDiagnostics.AddCount(activity, "codec.decode_count");
        }
        finally
        {
            GameObjectDefinitionResolutionDiagnostics.RecordElapsed(activity, "codec.decode_duration_ms", decodeStart);
        }

        return definition;
    }

    public MemoryStream Encode(GameObjectDefinition instance) => _codec.Encode(instance);
}
