using Hagalaz.Services.GameWorld.Network.Handshake.Messages;

namespace Hagalaz.Services.GameWorld.Network.Handshake;

public sealed class ClientHandshakeHandler : IClientHandshakeHandler
{
    public ClientHandshakeResponse Handle(ClientHandshakeRequest request) => new() { ReturnCode = 0 };
}
