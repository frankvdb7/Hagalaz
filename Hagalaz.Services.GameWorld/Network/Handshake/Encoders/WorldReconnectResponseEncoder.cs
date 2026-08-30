using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders;
using Raido.Common.Protocol;
using Raido.Server.Extensions;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Encoders;

public sealed class WorldReconnectResponseEncoder : IRaidoMessageEncoder<WorldReconnectResponse>
{
    private readonly ICharacterLocationService _characterLocationMap;

    public WorldReconnectResponseEncoder(ICharacterLocationService characterLocationMap) =>
        _characterLocationMap = characterLocationMap;

    public void EncodeMessage(WorldReconnectResponse message, IRaidoMessageBinaryWriter output)
    {
        output
            .SetOpcode(15)
            .SetSize(RaidoMessageSize.VariableShort);

        DrawStandardMapMessageEncoder.WritePlayerEntryBits(
            output,
            message.CharacterIndex,
            message.CharacterLocation,
            _characterLocationMap);
    }
}
