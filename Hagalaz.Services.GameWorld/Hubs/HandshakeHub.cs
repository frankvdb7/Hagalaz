using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Authorization.Constants;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Hagalaz.Services.GameWorld.Providers;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Polly.Timeout;
using Raido.Common.Protocol;
using Raido.Server;
using Hagalaz.Security.Extensions;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Hubs
{
    public class HandshakeHub : RaidoHub
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IClientPermissionProvider _clientPermissionProvider;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ISystemUpdateService _systemUpdate;
        private readonly IOptions<ServerConfig> _serverOptions;
        private readonly IOptions<WorldOptions> _worldOptions;
        private readonly IConfiguration _configuration;
        private readonly IScopedGameMediator _mediator;
        private readonly WorldLifecycleState _lifecycle;
        private readonly WorldRegistrationStore _registrations;
        private readonly WorldInstanceIdentity _identity;

        public HandshakeHub(
            IAuthenticationService authenticationService,
            IClientPermissionProvider clientPermissionProvider,
            IServiceScopeFactory scopeFactory,
            ISystemUpdateService systemUpdate,
            IOptions<ServerConfig> serverOptions,
            IOptions<WorldOptions> worldOptions,
            IConfiguration configuration,
            IScopedGameMediator mediator,
            WorldLifecycleState lifecycle,
            WorldRegistrationStore registrations,
            WorldInstanceIdentity identity,
            IGameSessionService gameSessionService,
            IGameSessionClaimStore sessionClaims,
            RaidoHubConnectionStore connections)
        {
            _authenticationService = authenticationService;
            _clientPermissionProvider = clientPermissionProvider;
            _scopeFactory = scopeFactory;
            _systemUpdate = systemUpdate;
            _serverOptions = serverOptions;
            _worldOptions = worldOptions;
            _configuration = configuration;
            _mediator = mediator;
            _lifecycle = lifecycle;
            _registrations = registrations;
            _identity = identity;
            _gameSessionService = gameSessionService;
            _sessionClaims = sessionClaims;
            _connections = connections;
        }

        private readonly IGameSessionService _gameSessionService;
        private readonly IGameSessionClaimStore _sessionClaims;
        private readonly RaidoHubConnectionStore _connections;

        [RaidoMessageHandler(typeof(ClientUpdateRequest))]
        public void HandleClientUpdate(ClientUpdateRequest message) =>
            // disconnect and let client forward to update server
            Context.Abort();

        [RaidoMessageHandler(typeof(ClientHandshakeRequest))]
        public ValueTask<ClientHandshakeResponse> HandleClientHandshake(ClientHandshakeRequest message) =>
            ValueTask.FromResult(new ClientHandshakeResponse()
            {
                ReturnCode = 0 // acknowledge return code
            });

        [RaidoMessageHandler(typeof(LobbySignInRequest))]
        public async Task SignInLobby(LobbySignInRequest message)
        {
            var protocolScope = _scopeFactory.CreateAsyncScope();
            var protocolScopeTransferred = false;
            try
            {
                var clientProtocol = protocolScope.ServiceProvider
                    .GetRequiredService<IClientProtocolResolver>()
                    .GetProtocol(message.ClientRevision);
                if (clientProtocol == null)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.Outdated);
                    Context.Abort();
                    return;
                }

                try
                {
                    var clientResponse = await SignInAsync(message, false);
                    if (!clientResponse.Succeeded)
                    {
                        await Clients.Caller.SendAsync(clientResponse);
                        Context.Abort();
                        return;
                    }
                }
                catch (RequestTimeoutException)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.AuthServiceOffline);
                    Context.Abort();
                    throw;
                }
                catch (Exception)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.Failed);
                    Context.Abort();
                    throw;
                }

                var session = Context.GetSession();
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (session == null)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.BadSession);
                    Context.Abort();
                    return;
                }

                var user = Context.User!;
                var masterId = Context.GetMasterId();
                if (masterId == null)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.BadSession);
                    Context.Abort();
                    return;
                }

                var worldId = _worldOptions.Value.Id;
                var roles = user.FindAllRoles().Select(claim => claim.Value).ToList();
                var clientPermission = _clientPermissionProvider.GetClientPermission(roles);
                var displayName = user.FindFirst(OpenIddictConstants.Claims.PreferredUsername)?.Value!;
                _ = DateTimeOffset.TryParse(user.FindFirst(Claims.LastLogin)?.Value, out var lastLogin);
                var lastIp = user.FindFirst(Claims.LastIp)?.Value;

                // the handshake protocol should still handle the response
                await Clients.Caller.SendAsync(new LobbySignInResponse()
                {
                    DisplayName = displayName,
                    ClientPermissions = clientPermission,
                    LastLogin = lastLogin,
                    LastIpAddress = lastIp,
                    UnreadMessagesCount = 0,
                    WorldId = worldId,
                    WorldAddress = _worldOptions.Value.AdvertisedEndpoint.Host
                });

                // now let the appropriate client protocol handle any communication
                clientProtocol.SetEncryptionSeed(message.IsaacSeed);
                protocolScopeTransferred = true;
                await Context.SetProtocolAsync(clientProtocol, protocolScope, CancellationToken.None);

                _mediator.Publish(new LobbySignInCommand(masterId.Value, session));
            }
            finally
            {
                if (!protocolScopeTransferred)
                {
                    await protocolScope.DisposeAsync();
                }
            }
        }

        [RaidoMessageHandler(typeof(WorldSignInRequest))]
        public async Task SignInWorld(WorldSignInRequest message)
        {
            var protocolScope = _scopeFactory.CreateAsyncScope();
            var protocolScopeTransferred = false;
            try
            {
                var worldOptions = _worldOptions.Value;
                if (!_lifecycle.CanAcceptWorldSignIns ||
                    _registrations.HasConflict(worldOptions.Id, _identity.InstanceId) ||
                    !_registrations.IsLocalGenerationAvailable(worldOptions.Id, _identity.InstanceId))
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.Failed);
                    Context.Abort();
                    return;
                }

                var clientProtocol = protocolScope.ServiceProvider
                    .GetRequiredService<IClientProtocolResolver>()
                    .GetProtocol(message.ClientRevision);
                if (clientProtocol == null)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.Outdated);
                    Context.Abort();
                    return;
                }

                try
                {
                    var clientResponse = await SignInAsync(message, true);
                    if (!clientResponse.Succeeded)
                    {
                        await Clients.Caller.SendAsync(clientResponse);
                        Context.Abort();
                        return;
                    }
                }
                catch (Exception ex) when (ex is RequestTimeoutException or TimeoutRejectedException)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.AuthServiceOffline);
                    Context.Abort();
                    throw;
                }
                catch (Exception)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.Failed);
                    Context.Abort();
                    throw;
                }

                var character = Context.GetCharacter();
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (character == null)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.BadSession);
                    Context.Abort();
                    return;
                }

                var user = Context.User!;
                var roles = user.FindAllRoles().Select(claim => claim.Value).ToList();
                var clientPermission = _clientPermissionProvider.GetClientPermission(roles);
                var displayName = user.FindFirst(OpenIddictConstants.Claims.PreferredUsername)?.Value!;

                clientProtocol.SetEncryptionSeed(message.IsaacSeed);
                var reconnectFeature = Context.Features.Get<IRaidoStatefulReconnectFeature>();
                if (reconnectFeature?.TryEnable() != true)
                {
                    Context.Abort();
                    return;
                }

                // the handshake protocol should still handle the response
                await Clients.Caller.SendAsync(new WorldSignInResponse()
                {
                    DisplayName = displayName,
                    ClientPermissions = clientPermission,
                    IsQuickChatOnly = false,
                    CharacterWorldIndex = character.Index,
                    IsMembersOnly = true
                });

                // now let the appropriate client protocol handle any communication
                protocolScopeTransferred = true;
                await Context.SetProtocolAsync(clientProtocol, protocolScope, CancellationToken.None);

                _mediator.Publish(new WorldSignInCommand(character));
            }
            finally
            {
                if (!protocolScopeTransferred)
                {
                    await protocolScope.DisposeAsync();
                }
            }
        }

        [RaidoMessageHandler(typeof(WorldReconnectRequest))]
        public async Task ReconnectWorld(WorldReconnectRequest message)
        {
            var protocolScope = _scopeFactory.CreateAsyncScope();
            var protocolScopeTransferred = false;
            try
            {
                var validation = ValidateHandshake(message);
                if (validation != ClientSignInResponse.Success)
                {
                    await Clients.Caller.SendAsync(validation);
                    Context.Abort();
                    return;
                }

                WorldReconnectAuthenticationResult authentication;
                try
                {
                    authentication = await _authenticationService.AuthenticateWorldReconnectAsync(message.Login, message.Password);
                }
                catch (Exception ex) when (ex is RequestTimeoutException or TimeoutRejectedException)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.AuthServiceOffline);
                    Context.Abort();
                    throw;
                }

                if (!authentication.Succeeded || authentication.MasterId is not uint masterId)
                {
                    await Clients.Caller.SendAsync(GetReconnectFailureResponse(authentication));
                    Context.Abort();
                    return;
                }

                var session = await _gameSessionService.FindWorldSessionByMasterId(masterId);
                var target = session == null ? null : _connections[session.ConnectionId];
                var candidate = _connections[Context.ConnectionId];
                if (session == null || target == null || candidate == null || ReferenceEquals(target, candidate) ||
                    target.ConnectionId != session.ConnectionId ||
                    !IsMatchingWorldConnection(target, session, masterId))
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.BadSession);
                    Context.Abort();
                    return;
                }

                var clientProtocol = protocolScope.ServiceProvider
                    .GetRequiredService<IClientProtocolResolver>()
                    .GetProtocol(message.ClientRevision);
                if (clientProtocol == null)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.Outdated);
                    Context.Abort();
                    return;
                }

                var character = target.Features.Get<ICharacterFeature>()?.Character;
                if (character == null)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.BadSession);
                    Context.Abort();
                    return;
                }

                clientProtocol.SetEncryptionSeed(message.IsaacSeed);
                var responseBytes = Context.Protocol.GetMessageBytes(new WorldReconnectResponse
                {
                    CharacterIndex = character.Index,
                    CharacterLocation = character.Location
                });
                if (responseBytes.IsEmpty)
                {
                    await Clients.Caller.SendAsync(ClientSignInResponse.Failed);
                    Context.Abort();
                    return;
                }

                var reconnected = await _sessionClaims.ExecuteIfOwnerAsync(
                    masterId,
                    session.SessionClaimId,
                    cancellationToken =>
                    {
                        // Once the claim owner invokes the transport operation, that operation owns the scope and
                        // disposes it on every unsuccessful handoff.
                        protocolScopeTransferred = true;
                        return target.TryReconnectAsync(
                            candidate,
                            responseBytes,
                            clientProtocol,
                            protocolScope,
                            cancellationToken).AsTask();
                    },
                    Context.ConnectionAborted);
                if (!reconnected)
                {
                    Context.Abort();
                    return;
                }

                character.GameClient.DisplayMode = message.DisplayMode;
                character.GameClient.Language = message.Language;
                character.GameClient.ScreenSizeX = message.ClientSizeX;
                character.GameClient.ScreenSizeY = message.ClientSizeY;
            }
            finally
            {
                if (!protocolScopeTransferred)
                {
                    await protocolScope.DisposeAsync();
                }
            }
        }

        private async ValueTask<ClientSignInResponse> SignInAsync(ClientSignInRequest request, bool isWorldSignIn)
        {
            var validation = ValidateHandshake(request);
            if (validation != ClientSignInResponse.Success)
            {
                return validation;
            }

            var signInResult = isWorldSignIn
                ? await _authenticationService.SignInWorldAsync(new SignInRequest
                {
                    Login = request.Login,
                    Password = request.Password,
                    GameClient = new GameClient(request.DisplayMode, request.Language, request.ClientSizeX, request.ClientSizeY)
                })
                : await _authenticationService.SignInLobbyAsync(new SignInRequest
                {
                    Login = request.Login,
                    Password = request.Password,
                    GameClient = new GameClient(request.DisplayMode, request.Language, request.ClientSizeX, request.ClientSizeY)
                });
            if (signInResult.IsDisabled)
            {
                return ClientSignInResponse.Disabled;
            }

            if (signInResult.IsAlreadyLoggedOn)
            {
                return ClientSignInResponse.AlreadyLoggedOn;
            }

            if (signInResult.AreCredentialsInvalid)
            {
                return ClientSignInResponse.CredentialsInvalid;
            }

            if (signInResult.IsLockedOut)
            {
                return ClientSignInResponse.Disabled;
            }

            if (!signInResult.Succeeded)
            {
                return ClientSignInResponse.BadSession;
            }

            return ClientSignInResponse.Success;
        }

        private ClientSignInResponse ValidateHandshake(ClientSignInRequest request)
        {
            var options = _serverOptions.Value;
            if (request.ClientRevision != options.ClientRevision || request.ClientRevisionPatch != options.ClientRevisionPatch)
            {
                return ClientSignInResponse.Outdated;
            }

            return _systemUpdate.SystemUpdateScheduled
                ? ClientSignInResponse.SystemUpdate
                : ClientSignInResponse.Success;
        }

        private static ClientSignInResponse GetReconnectFailureResponse(WorldReconnectAuthenticationResult result) =>
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
                ReferenceEquals(targetCharacter?.Session, session) &&
                targetAuthentication?.TryGetClaim<string>(OpenIddictConstants.Claims.Subject, out var subject) == true &&
                uint.TryParse(subject, out var authenticatedMasterId) &&
                authenticatedMasterId == masterId;
        }
    }
}
