using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Authorization.Messages;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Raido.Server;
using Hagalaz.Services.GameWorld.Extensions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Features_AuthenticationFeature = Hagalaz.Services.GameWorld.Features.AuthenticationFeature;
using Features_AuthenticationProperties = Hagalaz.Services.GameWorld.Features.AuthenticationProperties;
using Features_IAuthenticationFeature = Hagalaz.Services.GameWorld.Features.IAuthenticationFeature;

namespace Hagalaz.Services.GameWorld.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private static readonly ImmutableArray<string> _defaultScopes =
        [
            Scopes.OpenId,
            Scopes.Email,
            Scopes.Profile,
            Scopes.Roles,
            Scopes.OfflineAccess
        ];

        private static readonly ImmutableArray<string> _lobbyClientScopes = [Constants.OAuth.WorldClientId, Constants.OAuth.LobbyClientId];
        private static readonly ImmutableArray<string> _worldClientScopes = [Constants.OAuth.WorldClientId];

        private readonly ILogger<AuthenticationService> _logger;
        private readonly IMapper _mapper;
        private readonly ICharacterService _characterService;
        private readonly ICharacterFactory _characterFactory;
        private readonly ICharacterHydrationService _characterHydrationService;
        private readonly IGameSessionService _gameSessionService;
        private readonly IRequestClient<SignInUserRequestMessage> _signInUserRequestClient;
        private readonly IRequestClient<GetUserInfoRequestMessage> _getUserInfoRequestClient;
        private readonly IRequestClient<RevokeTokenRequestMessage> _revokeTokenRequestClient;
        private readonly IRequestClient<HydrateCharacter> _getCharacterRequestClient;
        private readonly IClaimsPrincipalFactory _claimsPrincipalFactory;
        private readonly IRaidoCallerContextAccessor _contextAccessor;
        private readonly IGameMediator _mediator;
        private readonly ResiliencePipeline _authLoginPipeline;
        private readonly ResiliencePipeline _authLogoutPipeline;

        public AuthenticationService(
            ILogger<AuthenticationService> logger,
            IMapper mapper,
            ICharacterService characterService,
            ICharacterFactory characterFactory,
            ICharacterHydrationService characterHydrator,
            IGameSessionService gameSessionService,
            IRequestClient<SignInUserRequestMessage> signInUserRequestClient,
            IRequestClient<GetUserInfoRequestMessage> getUserInfoRequestClient,
            IRequestClient<RevokeTokenRequestMessage> revokeTokenRequestClient,
            IRequestClient<HydrateCharacter> getCharacterRequestClient,
            IClaimsPrincipalFactory claimsPrincipalFactory,
            IRaidoCallerContextAccessor contextAccessor,
            IGameMediator mediator,
            [FromKeyedServices(Constants.Pipeline.AuthSignInPipeline)]
            ResiliencePipeline authLoginPipeline,
            [FromKeyedServices(Constants.Pipeline.AuthSignOutPipeline)]
            ResiliencePipeline authLogoutPipeline)
        {
            _logger = logger;
            _mapper = mapper;
            _characterService = characterService;
            _characterFactory = characterFactory;
            _characterHydrationService = characterHydrator;
            _gameSessionService = gameSessionService;
            _signInUserRequestClient = signInUserRequestClient;
            _getUserInfoRequestClient = getUserInfoRequestClient;
            _revokeTokenRequestClient = revokeTokenRequestClient;
            _getCharacterRequestClient = getCharacterRequestClient;
            _claimsPrincipalFactory = claimsPrincipalFactory;
            _contextAccessor = contextAccessor;
            _mediator = mediator;
            _authLoginPipeline = authLoginPipeline;
            _authLogoutPipeline = authLogoutPipeline;
        }

        public async ValueTask<SignInResult> SignInLobbyAsync(SignInRequest signInRequest) =>
            await _authLoginPipeline.ExecuteAsync(async (cancellationToken) =>
            {
                var result = await SignInAsync(signInRequest, Constants.OAuth.LobbyClientId, _lobbyClientScopes, cancellationToken);
                if (!result.Succeeded)
                {
                    return result;
                }

                var context = _contextAccessor.Context;
                var authentication = context.GetAuthentication();
                if (!authentication.AuthenticationProperties.TryGetClaim(Claims.Subject, out string? subject))
                {
                    return SignInResult.Fail;
                }

                var masterId = Convert.ToUInt32(subject);
                var session = await _gameSessionService.AddSession(masterId, context.ConnectionId);
                context.Features.Set<ISessionFeature>(new SessionFeature
                {
                    Session = session
                });
                context.Features.Set<IContactsFeature>(new LobbyContactsFeature());
                context.Features.Set<IUserProfileFeature>(new UserProfileFeature()); // TODO
                return result;
            });

        public async ValueTask<SignInResult> SignInWorldAsync(SignInRequest signInRequest) =>
            await _authLoginPipeline.ExecuteAsync(async cancellationToken =>
            {
                var characterCount = await _characterService.CountAsync();
                // TODO - character count / give donators extra queue
                if (characterCount >= 2000)
                {
                    return SignInResult.Full;
                }

                var result = await SignInAsync(signInRequest, Constants.OAuth.WorldClientId, _worldClientScopes, cancellationToken);
                if (!result.Succeeded)
                {
                    return result;
                }

                var context = _contextAccessor.Context;
                var authentication = context.GetAuthentication();
                if (!authentication.AuthenticationProperties.TryGetClaim(Claims.Subject, out string? subject))
                {
                    return SignInResult.Fail;
                }

                var masterId = Convert.ToUInt32(subject);
                CharacterModel characterModel;
                try
                {
                    var response = await _getCharacterRequestClient.GetResponse<CharacterHydrated, CharacterNotFound>(new HydrateCharacter(masterId),
                        cancellationToken);
                    if (response.Is<CharacterNotFound>(out var notFoundResult))
                    {
                        return SignInResult.Fail;
                    }

                    if (response.Is<CharacterHydrated>(out var hydrateCharacterResult))
                    {
                        characterModel = _mapper.Map<CharacterModel>(hydrateCharacterResult.Message);
                        characterModel = characterModel with
                        {
                            Claims = _mapper.Map<HydratedClaims>(authentication.AuthenticationProperties)
                        };
                    }
                    else
                    {
                        _logger.LogError("Failed to get valid hydrate character response '{type}'", response.Message.GetType());
                        return SignInResult.Fail;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get hydrate character response");
                    return SignInResult.Fail;
                }

                var session = await _gameSessionService.AddSession(masterId, context.ConnectionId);
                var character = _characterFactory.Create(session, signInRequest.GameClient);
                if (!await _characterHydrationService.HydrateAsync(character, characterModel))
                {
                    _logger.LogWarning("Unable to hydrate character '{character}'", character);
                    return SignInResult.Fail;
                }

                if (!await _characterService.AddAsync(character))
                {
                    _logger.LogWarning("Unable to add character '{character}'", character);
                    return SignInResult.Fail;
                }

                context.Features.Set<ICharacterFeature>(new CharacterFeature
                {
                    Character = character
                });
                context.Features.Set<ISessionFeature>(new SessionFeature
                {
                    Session = session
                });
                context.Features.Set<IContactsFeature>(new WorldContactsFeature(character));
                context.Features.Set<IUserProfileFeature>(new UserProfileFeature()); // TODO
                return result;
            });

        private async ValueTask<SignInResult> SignInAsync(
            SignInRequest signInRequest, string clientId, ImmutableArray<string> clientScopes, CancellationToken cancellationToken)
        {
            var context = _contextAccessor.Context;
            var signInResponse = await _signInUserRequestClient.GetResponse<SignInUserResponseMessage>(new SignInUserRequestMessage(signInRequest.Login,
                    signInRequest.Password,
                    context.RemoteIPEndPoint!.Address.ToString(),
                    clientId,
                    _defaultScopes,
                    clientScopes),
                cancellationToken);
            var signInMessage = signInResponse.Message;
            if (signInMessage.Succeeded)
            {
                var userInfoResponse =
                    await _getUserInfoRequestClient.GetResponse<GetUserInfoResponseMessage>(new GetUserInfoRequestMessage(signInMessage.AccessToken),
                        cancellationToken);
                var userInfoMessage = userInfoResponse.Message;
                if (userInfoMessage.Claims == null)
                {
                    return SignInResult.Fail;
                }

                var user = _claimsPrincipalFactory.Create(userInfoMessage.Claims);
                if (user.Identity == null || !user.Identity.IsAuthenticated)
                {
                    return SignInResult.Fail;
                }

                var properties = new Features_AuthenticationProperties
                {
                    ClientId = clientId,
                    IdToken = signInMessage.IdToken,
                    AccessToken = signInMessage.AccessToken,
                    ExpireDate = signInMessage.ExpireDate,
                    Scope = signInMessage.Scope,
                    TokenType = signInMessage.TokenType,
                    Claims = userInfoMessage.Claims
                };
                var authenticationFeature = new Features_AuthenticationFeature
                {
                    AuthenticationProperties = properties, User = user
                };
                context.Features.Set<Features_IAuthenticationFeature>(authenticationFeature);
                context.Features.Set<IConnectionUserFeature>(authenticationFeature);
                return SignInResult.Success;
            }

            if (signInMessage.IsDisabled)
            {
                return SignInResult.Disabled;
            }

            if (signInMessage.AreCredentialsInvalid)
            {
                return SignInResult.CredentialsInvalid;
            }

            if (signInMessage.IsAuthenticated)
            {
                return SignInResult.AlreadyLoggedOn;
            }

            if (signInMessage.IsLockedOut)
            {
                return SignInResult.LockedOut;
            }

            return SignInResult.Fail;
        }

        public async Task SignOutAsync() =>
            await _authLogoutPipeline.ExecuteAsync(async (cancellationToken) =>
            {
                var context = _contextAccessor.Context;
                var masterId = context.GetMasterId();
                var authentication = context.GetAuthentication();
                var properties = authentication?.AuthenticationProperties;
                if (masterId != null && properties?.ClientId != null)
                {
                    var response = await _revokeTokenRequestClient.GetResponse<RevokeTokenResponseMessage>(
                        new RevokeTokenRequestMessage(properties.ClientId, masterId.Value.ToString()),
                        cancellationToken);
                    if (!response.Message.Succeeded)
                    {
                        _logger.LogWarning("Failed to revoke token '{error}'", response.Message.Error);
                    }
                }

                var session = context.GetSession();
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (session != null)
                {
                    await _gameSessionService.RemoveSession(session.ConnectionId);
                }

                var character = context.GetCharacter();
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (character != null)
                {
                    if (!await _characterService.RemoveAsync(character))
                    {
                        _logger.LogWarning("Failed to remove character '{character}'", character);
                    }

                    _mediator.Publish(new WorldSignOutCommand(character.MasterId));
                }
                else if (masterId != null)
                {
                    _mediator.Publish(new LobbySignOutCommand(masterId.Value));
                }
            });
    }
}