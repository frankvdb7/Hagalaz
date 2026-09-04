using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Network.Handshake;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Hagalaz.Services.GameWorld.Providers;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Raido.Common.Messages;
using Raido.Common.Protocol;
using Raido.Server;
using Polly.Timeout;
using MassTransit;

namespace Hagalaz.Services.GameWorld.Network;

public sealed class WorldReconnectConnectionHandler
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IGameSessionService _gameSessionService;
    private readonly IGameSessionClaimStore _sessionClaims;
    private readonly RaidoHubConnectionStore _connections;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHandshakeValidator<WorldReconnectRequest> _handshakeValidator;
    private readonly ILogger<WorldReconnectConnectionHandler> _logger;

    public WorldReconnectConnectionHandler(
        IAuthenticationService authenticationService,
        IGameSessionService gameSessionService,
        IGameSessionClaimStore sessionClaims,
        RaidoHubConnectionStore connections,
        IServiceScopeFactory scopeFactory,
        IHandshakeValidator<WorldReconnectRequest> handshakeValidator,
        ILogger<WorldReconnectConnectionHandler> logger)
    {
        _authenticationService = authenticationService;
        _gameSessionService = gameSessionService;
        _sessionClaims = sessionClaims;
        _connections = connections;
        _scopeFactory = scopeFactory;
        _handshakeValidator = handshakeValidator;
        _logger = logger;
    }

    public async Task HandleAsync(
        ConnectionContext connection,
        HandshakeProtocol handshakeProtocol,
        WorldReconnectRequest message,
        CancellationToken cancellationToken)
    {
        var protocolScope = _scopeFactory.CreateAsyncScope();
        var protocolScopeTransferred = false;
        try
        {
            var validation = _handshakeValidator.Validate(message);
            if (validation != ClientSignInResponse.Success)
            {
                await SendResponseAsync(connection, handshakeProtocol, validation, cancellationToken);
                connection.Abort();
                return;
            }

            WorldReconnectAuthenticationResult authentication;
            try
            {
                authentication = await _authenticationService.AuthenticateWorldReconnectAsync(message.Login, message.Password);
            }
            catch (RequestTimeoutException)
            {
                await SendResponseAsync(connection, handshakeProtocol, ClientSignInResponse.AuthServiceOffline, cancellationToken);
                connection.Abort();
                return;
            }
            catch (TimeoutRejectedException)
            {
                await SendResponseAsync(connection, handshakeProtocol, ClientSignInResponse.AuthServiceOffline, cancellationToken);
                connection.Abort();
                return;
            }

            if (!authentication.Succeeded || authentication.MasterId is not uint)
            {
                await SendResponseAsync(connection, handshakeProtocol, GetFailureResponse(authentication), cancellationToken);
                connection.Abort();
                return;
            }

            var masterId = authentication.MasterId.Value;

            var session = await _gameSessionService.FindWorldSessionByMasterId(masterId);
            var target = session is null ? null : _connections[session.ConnectionId];
            if (session is null || target is null || target.ConnectionId != session.ConnectionId ||
                !IsMatchingWorldConnection(target, session, masterId))
            {
                await SendResponseAsync(connection, handshakeProtocol, ClientSignInResponse.BadSession, cancellationToken);
                connection.Abort();
                return;
            }

            var clientProtocol = protocolScope.ServiceProvider
                .GetRequiredService<IClientProtocolResolver>()
                .GetProtocol(message.ClientRevision);
            if (clientProtocol is null)
            {
                await SendResponseAsync(connection, handshakeProtocol, ClientSignInResponse.Outdated, cancellationToken);
                connection.Abort();
                return;
            }

            var character = target.Features.Get<ICharacterFeature>()?.Character;
            if (character is null)
            {
                await SendResponseAsync(connection, handshakeProtocol, ClientSignInResponse.BadSession, cancellationToken);
                connection.Abort();
                return;
            }

            clientProtocol.SetEncryptionSeed(message.IsaacSeed);
            var attached = await _sessionClaims.ExecuteIfOwnerAsync(
                masterId,
                session.SessionClaimId,
                async _ =>
                {
                    await target.SetProtocolAsync(clientProtocol, protocolScope, CancellationToken.None);
                    protocolScopeTransferred = true;

                    await SendResponseAsync(
                        connection,
                        handshakeProtocol,
                        new WorldReconnectResponse
                        {
                            CharacterIndex = character.Index,
                            CharacterLocation = character.Location
                        },
                        CancellationToken.None);

                    return target.TryAttachPhysicalConnection(connection);
                },
                connection.ConnectionClosed);
            if (!attached)
            {
                connection.Abort();
                return;
            }

            character.GameClient.DisplayMode = message.DisplayMode;
            character.GameClient.Language = message.Language;
            character.GameClient.ScreenSizeX = message.ClientSizeX;
            character.GameClient.ScreenSizeY = message.ClientSizeY;
        }
        catch (OperationCanceledException) when (connection.ConnectionClosed.IsCancellationRequested)
        {
            connection.Abort();
        }
        catch (Exception ex)
        {
            Log.Failed(_logger, ex);
            connection.Abort(new ConnectionAbortedException("World reconnect failed.", ex));
        }
        finally
        {
            if (!protocolScopeTransferred)
            {
                await protocolScope.DisposeAsync();
            }
        }
    }

    private static async Task SendResponseAsync<TResponse>(
        ConnectionContext connection,
        HandshakeProtocol protocol,
        TResponse response,
        CancellationToken cancellationToken)
        where TResponse : RaidoMessage
    {
        protocol.WriteMessage(response, connection.Transport.Output);
        var result = await connection.Transport.Output.FlushAsync(cancellationToken);
        if (result.IsCanceled)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private static ClientSignInResponse GetFailureResponse(WorldReconnectAuthenticationResult result) =>
        result.IsDisabled || result.IsLockedOut
            ? ClientSignInResponse.Disabled
            : result.AreCredentialsInvalid
                ? ClientSignInResponse.CredentialsInvalid
                : ClientSignInResponse.BadSession;

    private static bool IsMatchingWorldConnection(
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

    private static class Log
    {
        private static readonly Action<ILogger, Exception> _failed = LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1, "ReconnectFailed"),
            "World reconnect failed.");

        public static void Failed(ILogger logger, Exception exception) => _failed(logger, exception);
    }
}
