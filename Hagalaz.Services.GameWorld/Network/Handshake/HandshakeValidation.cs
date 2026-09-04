using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Raido.Server;

namespace Hagalaz.Services.GameWorld.Network.Handshake;

internal static class HandshakeValidation
{
    public static ClientSignInResponse Validate(
        ClientSignInRequest request,
        IOptions<ServerConfig> serverOptions,
        ISystemUpdateService systemUpdate)
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

    public static ClientSignInResponse GetReconnectFailureResponse(WorldReconnectAuthenticationResult result) =>
        result.IsDisabled || result.IsLockedOut
            ? ClientSignInResponse.Disabled
            : result.AreCredentialsInvalid
                ? ClientSignInResponse.CredentialsInvalid
                : ClientSignInResponse.BadSession;

    public static bool IsMatchingWorldConnection(
        RaidoHubConnectionContext target,
        IGameWorldSession session,
        uint masterId)
    {
        var targetSession = target.Features.Get<ISessionFeature>()?.Session;
        var targetCharacter = target.Features.Get<ICharacterFeature>()?.Character;
        var targetAuthentication = target.Features.Get<IAuthenticationFeature>()?.AuthenticationProperties;
        return ReferenceEquals(targetSession, session) &&
            targetSession is IGameWorldSession targetWorldSession &&
            targetWorldSession.MasterId == masterId &&
            targetWorldSession.SessionClaimId == session.SessionClaimId &&
            targetCharacter?.MasterId == masterId &&
            ReferenceEquals(targetCharacter.Session, session) &&
            targetAuthentication?.TryGetClaim<string>(OpenIddictConstants.Claims.Subject, out var subject) == true &&
            uint.TryParse(subject, out var authenticatedMasterId) &&
            authenticatedMasterId == masterId;
    }
}
