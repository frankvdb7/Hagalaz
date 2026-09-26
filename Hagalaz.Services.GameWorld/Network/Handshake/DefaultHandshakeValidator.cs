using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Network.Handshake;

internal sealed class DefaultHandshakeValidator(
    IOptions<ServerConfig> serverOptions,
    ISystemUpdateService systemUpdate) : IHandshakeValidator
{
    public ClientSignInResponse Validate(ClientSignInRequest request)
    {
        var options = serverOptions.Value;
        if (request.ClientRevision != options.ClientRevision || request.ClientRevisionPatch != options.ClientRevisionPatch)
        {
            return ClientSignInResponse.Outdated;
        }

        return systemUpdate.SystemUpdateScheduled
            ? ClientSignInResponse.SystemUpdate
            : ClientSignInResponse.Success;
    }
}
