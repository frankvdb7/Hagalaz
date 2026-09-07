using Hagalaz.Services.GameWorld.Network.Handshake;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;

namespace Hagalaz.Services.GameWorld.Tests;

internal sealed class TestHandshakeValidator : IHandshakeValidator
{
    private readonly Func<ClientSignInRequest, ClientSignInResponse> _validate;

    public TestHandshakeValidator(ClientSignInResponse response)
        : this(_ => response)
    {
    }

    public TestHandshakeValidator(Func<ClientSignInRequest, ClientSignInResponse> validate)
    {
        _validate = validate;
    }

    public ClientSignInResponse Validate(ClientSignInRequest request) => _validate(request);
}
