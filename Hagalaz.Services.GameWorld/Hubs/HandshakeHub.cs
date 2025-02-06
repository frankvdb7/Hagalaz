using System;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Authorization.Constants;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
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
        private readonly IClientPermissionProvider _clientPermissionProvider;
        private readonly IClientProtocolResolver _clientProtocolResolver;
        private readonly ISystemUpdateService _systemUpdate;
        private readonly IOptions<ServerConfig> _serverOptions;
        private readonly IOptions<WorldOptions> _worldOptions;
        private readonly IConfiguration _configuration;
        private readonly IScopedGameMediator _mediator;

        public HandshakeHub(
            IAuthenticationService authenticationService,
            IClientPermissionProvider clientPermissionProvider,
            IClientProtocolResolver clientProtocolResolver,
            ISystemUpdateService systemUpdate,
            IOptions<ServerConfig> serverOptions,
            IOptions<WorldOptions> worldOptions,
            IConfiguration configuration,
            IScopedGameMediator mediator)
        {
            _authenticationService = authenticationService;
            _clientPermissionProvider = clientPermissionProvider;
            _clientProtocolResolver = clientProtocolResolver;
            _systemUpdate = systemUpdate;
            _serverOptions = serverOptions;
            _worldOptions = worldOptions;
            _configuration = configuration;
            _mediator = mediator;
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

            var url = "127.0.0.1"; // TODO
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
                WorldAddress = url
            });

            // now let the appropriate client protocol handle any communication
            clientProtocol.SetEncryptionSeed(message.IsaacSeed);
            Context.Protocol = clientProtocol;

            _mediator.Publish(new LobbySignInCommand(masterId.Value, session));
        }

        [RaidoMessageHandler(typeof(WorldSignInRequest))]
        public async Task SignInWorld(WorldSignInRequest message)
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

        private async ValueTask<ClientSignInResponse> SignInAsync(ClientSignInRequest request, bool isWorldSignIn)
        {
            var options = _serverOptions.Value;
            if (request.ClientRevision != options.ClientRevision || request.ClientRevisionPatch != options.ClientRevisionPatch)
            {
                return ClientSignInResponse.Outdated;
            }

            if (_systemUpdate.SystemUpdateScheduled)
            {
                return ClientSignInResponse.SystemUpdate;
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