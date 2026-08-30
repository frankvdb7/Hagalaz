using System;
using System.Linq;
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
using Hagalaz.Services.GameWorld.Network.Model;
using Hagalaz.Services.GameWorld.Network.Handshake.Messages;
using Hagalaz.Services.GameWorld.Providers;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;
using Microsoft.Extensions.Configuration;
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
        private readonly IGameConnectionService _gameConnectionService;
        private readonly IClientPermissionProvider _clientPermissionProvider;
        private readonly IClientProtocolResolver _clientProtocolResolver;
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
            IGameConnectionService gameConnectionService,
            IClientPermissionProvider clientPermissionProvider,
            IClientProtocolResolver clientProtocolResolver,
            ISystemUpdateService systemUpdate,
            IOptions<ServerConfig> serverOptions,
            IOptions<WorldOptions> worldOptions,
            IConfiguration configuration,
            IScopedGameMediator mediator,
            WorldLifecycleState lifecycle,
            WorldRegistrationStore registrations,
            WorldInstanceIdentity identity)
        {
            _authenticationService = authenticationService;
            _gameConnectionService = gameConnectionService;
            _clientPermissionProvider = clientPermissionProvider;
            _clientProtocolResolver = clientProtocolResolver;
            _systemUpdate = systemUpdate;
            _serverOptions = serverOptions;
            _worldOptions = worldOptions;
            _configuration = configuration;
            _mediator = mediator;
            _lifecycle = lifecycle;
            _registrations = registrations;
            _identity = identity;
        }

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
            var clientProtocol = _clientProtocolResolver.GetProtocol(message.ClientRevision);
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
            Context.Protocol = clientProtocol;

            _mediator.Publish(new LobbySignInCommand(masterId.Value, session));
        }

        [RaidoMessageHandler(typeof(WorldSignInRequest))]
        public async Task SignInWorld(WorldSignInRequest message)
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

            var clientProtocol = _clientProtocolResolver.GetProtocol(message.ClientRevision);
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
            clientProtocol.SetEncryptionSeed(message.IsaacSeed);
            Context.Protocol = clientProtocol;

            _mediator.Publish(new WorldSignInCommand(character));
        }

        [RaidoMessageHandler(typeof(WorldReconnectRequest))]
        public async Task ReconnectWorld(WorldReconnectRequest message)
        {
            var validation = ValidateHandshake(message);
            if (!validation.Succeeded)
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
            catch (Exception)
            {
                await Clients.Caller.SendAsync(ClientSignInResponse.Failed);
                Context.Abort();
                throw;
            }

            if (!authentication.Succeeded || authentication.MasterId is not uint masterId)
            {
                await Clients.Caller.SendAsync(ToClientResponse(authentication));
                Context.Abort();
                return;
            }

            var existingConnection = await _gameConnectionService.FindByMasterId(masterId);
            if (existingConnection is null || !OwnsExistingWorldSession(existingConnection, masterId))
            {
                await Clients.Caller.SendAsync(ClientSignInResponse.BadSession);
                Context.Abort();
                return;
            }

            var clientProtocol = _clientProtocolResolver.GetProtocol(message.ClientRevision);
            if (clientProtocol is null)
            {
                await Clients.Caller.SendAsync(ClientSignInResponse.Outdated);
                Context.Abort();
                return;
            }

            var character = existingConnection.Features.Get<ICharacterFeature>()!.Character;
            clientProtocol.SetEncryptionSeed(message.IsaacSeed);
            var response = new WorldReconnectResponse
            {
                CharacterIndex = character.Index,
                CharacterLocation = character.Location
            };

            var reconnected = await existingConnection.TryReconnectAsync(
                Context,
                clientProtocol,
                () => Context.WriteHandshakeAsync(response, Context.ConnectionAbortedToken));
            if (!reconnected)
            {
                Context.Abort();
            }
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

        private static bool OwnsExistingWorldSession(IGameConnection connection, uint masterId)
        {
            var authentication = connection.Features.Get<IAuthenticationFeature>();
            var session = connection.Features.Get<ISessionFeature>()?.Session;
            var character = connection.Features.Get<ICharacterFeature>()?.Character;
            var subject = authentication?.AuthenticationProperties.GetClaim<string>(OpenIddictConstants.Claims.Subject);
            return uint.TryParse(subject, out var authenticatedMasterId) &&
                authenticatedMasterId == masterId &&
                session is IGameWorldSession { MasterId: var sessionMasterId } && sessionMasterId == masterId &&
                character is not null && character.MasterId == masterId;
        }

        private static ClientSignInResponse ToClientResponse(WorldReconnectAuthenticationResult result)
        {
            if (result.IsDisabled)
            {
                return ClientSignInResponse.Disabled;
            }

            if (result.AreCredentialsInvalid)
            {
                return ClientSignInResponse.CredentialsInvalid;
            }

            if (result.IsLockedOut)
            {
                return ClientSignInResponse.LockedOut;
            }

            return ClientSignInResponse.BadSession;
        }

        private async ValueTask<ClientSignInResponse> SignInAsync(ClientSignInRequest request, bool isWorldSignIn)
        {
            var validation = ValidateHandshake(request);
            if (!validation.Succeeded)
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
    }
}
