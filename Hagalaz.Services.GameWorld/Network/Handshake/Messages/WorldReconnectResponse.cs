using Hagalaz.Game.Abstractions.Model;
using Raido.Common.Protocol;

namespace Hagalaz.Services.GameWorld.Network.Handshake.Messages;

public sealed class WorldReconnectResponse : RaidoMessage
{
    public required int CharacterIndex { get; init; }
    public required ILocation CharacterLocation { get; init; }
}
