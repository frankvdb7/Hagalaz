using Hagalaz.Services.GameWorld.Network.Handshake.Messages;

namespace Hagalaz.Services.GameWorld.Network.Handshake;

internal interface IHandshakeValidator
{
    ClientSignInResponse Validate(ClientSignInRequest request);
}
