using Hagalaz.Services.GameWorld.Network.Handshake.Messages;

namespace Hagalaz.Services.GameWorld.Network.Handshake;

public interface IHandshakeValidator<in TRequest>
    where TRequest : ClientSignInRequest
{
    ClientSignInResponse Validate(TRequest request);
}
