using Hagalaz.Services.GameWorld.Network.Handshake.Messages;

namespace Hagalaz.Services.GameWorld.Network.Handshake;

public interface IClientHandshakeHandler
{
    ClientHandshakeResponse Handle(ClientHandshakeRequest request);
}
