using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Network.Handshake;

internal sealed class DefaultHandshakeValidator<TRequest>(
    IOptions<ServerConfig> serverOptions,
    ISystemUpdateService systemUpdate) : IHandshakeValidator<TRequest>
    where TRequest : ClientSignInRequest
{
    public ClientSignInResponse Validate(TRequest request)
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
