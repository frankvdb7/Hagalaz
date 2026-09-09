using System;
using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Authorization.Messages;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Services.Model;
using Hagalaz.Services.GameWorld.Model;
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
        private readonly ICharacterPersistenceService _characterPersistenceService;
        private readonly ICharacterLogoutService _characterLogoutService;
        private readonly IGameSessionService _gameSessionService;
        private readonly IRequestClient<SignInUserRequestMessage> _signInUserRequestClient;
        private readonly IRequestClient<ValidateExistingAuthenticationRequestMessage> _validateExistingAuthenticationRequestClient;
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
            ICharacterPersistenceService characterPersistenceService,
            ICharacterLogoutService characterLogoutService,
            IGameSessionService gameSessionService,
            IRequestClient<SignInUserRequestMessage> signInUserRequestClient,
            IRequestClient<ValidateExistingAuthenticationRequestMessage> validateExistingAuthenticationRequestClient,
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
            _characterPersistenceService = characterPersistenceService;
            _characterLogoutService = characterLogoutService;
            _gameSessionService = gameSessionService;
            _signInUserRequestClient = signInUserRequestClient;
            _validateExistingAuthenticationRequestClient = validateExistingAuthenticationRequestClient;
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
            await ExecuteSignInAsync(async cancellationToken =>
            {
                var result = await SignInAsync(signInRequest, Constants.OAuth.LobbyClientId, _lobbyClientScopes, cancellationToken);
                if (!result.Succeeded)
                {
                    return result;
                }

                var context = _contextAccessor.Context;
                var masterId = context.GetMasterId();
                if (masterId is null)
                {
                    await RevokeCurrentAuthenticationAsync("lobby sign-in did not produce a valid subject");
                    return SignInResult.Fail;
                }

                (IGameSession Session, bool Created) sessionRegistration;
                try
                {
                    sessionRegistration = await _gameSessionService.AddSession(masterId.Value, context.ConnectionId);
                }
                catch
                {
                    await RevokeCurrentAuthenticationAsync("lobby session registration failed");
                    throw;
                }

                if (!sessionRegistration.Created)
                {
                    await RevokeCurrentAuthenticationAsync("lobby session ownership was already taken");
                    return SignInResult.AlreadyLoggedOn;
                }

                var session = sessionRegistration.Session;
                context.Features.Set<ISessionFeature>(new SessionFeature
                {
                    Session = session
                });
                context.Features.Set<IContactsFeature>(new LobbyContactsFeature());
                context.Features.Set<IUserProfileFeature>(new UserProfileFeature()); // TODO
                return result;
            });

        public async ValueTask<SignInResult> SignInWorldAsync(SignInRequest signInRequest) =>
            await ExecuteSignInAsync(async cancellationToken =>
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
                var masterId = context.GetMasterId();
                if (masterId is null)
                {
                    await RevokeCurrentAuthenticationAsync("world sign-in did not produce a valid subject");
                    return SignInResult.Fail;
                }
                var authentication = context.GetAuthentication();

                (IGameSession? Session, bool Created) sessionRegistration;
                try
                {
                    sessionRegistration = await _gameSessionService.TryAddWorldSession(masterId.Value, context.ConnectionId, cancellationToken);
                }
                catch
                {
                    await RevokeCurrentAuthenticationAsync("world session registration failed");
                    throw;
                }

                if (!sessionRegistration.Created || sessionRegistration.Session == null)
                {
                    await RevokeCurrentAuthenticationAsync("world session ownership was already taken");
                    return SignInResult.AlreadyLoggedOn;
                }

                var session = sessionRegistration.Session;
                var signInSucceeded = false;
                var characterRegistered = false;
                var revisionInitialized = false;
                ICharacter? registeredCharacter = null;
                try
                {
                    CharacterModel characterModel;
                    try
                    {
                        var response = await _getCharacterRequestClient.GetResponse<CharacterHydrated, CharacterNotFound>(new HydrateCharacter(masterId.Value),
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

                    var character = _characterFactory.Create(session, signInRequest.GameClient);
                    registeredCharacter = character;
                    if (!await _characterHydrationService.HydrateAsync(character, characterModel))
                    {
                        _logger.LogWarning("Unable to hydrate character '{character}'", character);
                        return SignInResult.Fail;
                    }

                    _characterPersistenceService.InitializeRevision(masterId.Value, characterModel.SnapshotRevision);
                    revisionInitialized = true;

                    if (!await _characterService.AddAsync(character))
                    {
                        _logger.LogWarning("Unable to add character '{character}'", character);
                        return SignInResult.Fail;
                    }

                    characterRegistered = true;
                    if (!await _gameSessionService.CommitWorldSession(session, cancellationToken))
                    {
                        _logger.LogWarning("Unable to commit world session '{connectionId}' after character registration", session.ConnectionId);
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
                    signInSucceeded = true;
                    return result;
                }
                finally
                {
                    if (!signInSucceeded && sessionRegistration.Created)
                    {
                        if (registeredCharacter is not null)
                        {
                            if (characterRegistered)
                            {
                                try
                                {
                                    if (await _characterService.RemoveAsync(registeredCharacter))
                                    {
                                        try
                                        {
                                            _characterPersistenceService.Forget(masterId.Value);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, "Failed to forget character persistence state after world sign-in failed");
                                        }

                                        try
                                        {
                                            registeredCharacter.Destroy();
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, "Failed to destroy character after world sign-in failed");
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Character '{MasterId}' removal returned false after world sign-in failed; retaining persistence state for recovery", masterId);
                                    }
                                }
                                catch (OperationCanceledException ex)
                                {
                                    _logger.LogError(ex, "Character removal was canceled after world sign-in failed");
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    _logger.LogError(ex, "Failed to remove character after world sign-in failed");
                                }
                            }
                            else
                            {
                                try
                                {
                                    registeredCharacter.Destroy();
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to destroy unregistered character after world sign-in failed");
                                }
                            }
                        }

                        if (!characterRegistered && revisionInitialized)
                        {
                            try
                            {
                                if (await _characterService.FindByMasterId(masterId.Value) == null)
                                {
                                    _characterPersistenceService.Forget(masterId.Value);
                                }
                                else
                                {
                                    _logger.LogWarning("Character '{MasterId}' was already registered after world sign-in failed; retaining persistence state for the existing character", masterId);
                                }
                            }
                            catch (OperationCanceledException ex)
                            {
                                _logger.LogError(ex, "Unable to determine character registration after world sign-in failed; retaining persistence state");
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                _logger.LogError(ex, "Unable to determine character registration after world sign-in failed; retaining persistence state");
                            }
                        }

                        try
                        {
                            // Cleanup must remain possible after request cancellation so an
                            // acquired claim cannot remain until its lease expires.
                            await _gameSessionService.RemoveSession(sessionRegistration.Session, CancellationToken.None);
                        }
                        catch (OperationCanceledException ex)
                        {
                            _logger.LogError(ex, "Game-session removal was canceled after world sign-in failed");
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            _logger.LogError(ex, "Failed to remove game session '{connectionId}' after world sign-in failed", session.ConnectionId);
                        }
                        finally
                        {
                            try
                            {
                                await _gameSessionService.RemoveLocalSession(sessionRegistration.Session);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to remove local game session '{connectionId}' after world sign-in failed", session.ConnectionId);
                            }
                        }

                        await RevokeCurrentAuthenticationAsync("world sign-in initialization failed");
                    }
                }
            });

        public async ValueTask<WorldReconnectAuthenticationResult> AuthenticateWorldReconnectAsync(WorldReconnectAuthenticationRequest request) =>
            await ExecuteSignInAsync(GetSignInPartitionKey(request), async cancellationToken =>
            {
                var response = await _validateExistingAuthenticationRequestClient.GetResponse<ValidateExistingAuthenticationResponseMessage>(
                    new ValidateExistingAuthenticationRequestMessage(
                        request.Login,
                        request.Password,
                        request.RemoteAddress?.ToString(),
                        _defaultScopes,
                        _worldClientScopes),
                    cancellationToken);
                var result = response.Message;
                if (result.Succeeded && uint.TryParse(result.Subject, out var masterId))
                {
                    return WorldReconnectAuthenticationResult.Success(masterId);
                }

                return WorldReconnectAuthenticationResult.FromValidation(
                    result.IsLockedOut,
                    result.IsDisabled,
                    result.AreCredentialsInvalid);
            });

        private async ValueTask<TResult> ExecuteSignInAsync<TResult>(
            Func<CancellationToken, ValueTask<TResult>> signIn)
            => await ExecuteSignInAsync(GetSignInPartitionKey(), signIn);

        private async ValueTask<TResult> ExecuteSignInAsync<TResult>(
            string partitionKey,
            Func<CancellationToken, ValueTask<TResult>> signIn)
        {
            var resilienceContext = ResilienceContextPool.Shared.Get();
            resilienceContext.Properties.Set(AuthenticationRateLimiting.PartitionKey, partitionKey);
            try
            {
                return await _authLoginPipeline.ExecuteAsync(
                    context => signIn(context.CancellationToken), resilienceContext);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(resilienceContext);
            }
        }

        private string GetSignInPartitionKey()
        {
            var context = _contextAccessor.Context;
            return context.RemoteIPEndPoint?.Address is { } address
                ? $"ip:{address}"
                : $"connection:{context.ConnectionId}";
        }

        private static string GetSignInPartitionKey(WorldReconnectAuthenticationRequest request) =>
            request.RemoteAddress is { } address
                ? $"ip:{address}"
                : $"connection:{request.ConnectionId}";

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
                context.Features.Set<PendingAuthorizationCleanup>(new PendingAuthorizationCleanup(
                    clientId,
                    signInMessage.Subject,
                    signInMessage.AuthorizationId));
                var authenticationCommitted = false;
                try
                {
                    var userInfoResponse =
                        await _getUserInfoRequestClient.GetResponse<GetUserInfoResponseMessage>(new GetUserInfoRequestMessage(signInMessage.AccessToken),
                            cancellationToken);
                    var userInfoMessage = userInfoResponse.Message;
                    if (userInfoMessage.Claims == null)
                    {
                        return SignInResult.Fail;
                    }

                    if (!userInfoMessage.Claims.TryGetValue(Claims.Subject, out var subjectValue) ||
                        subjectValue is not string subject ||
                        !uint.TryParse(subject, out _))
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
                        AuthorizationId = signInMessage.AuthorizationId,
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
                    context.Features.Set<PendingAuthorizationCleanup>(null);
                    authenticationCommitted = true;
                    return SignInResult.Success;
                }
                finally
                {
                    if (!authenticationCommitted)
                    {
                        await RevokePendingAuthorizationAsync("post-issuance authentication validation failed");
                    }
                }
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

        private async Task RevokeCurrentAuthenticationAsync(string reason)
        {
            var context = _contextAccessor.Context;
            var authentication = context.Features.Get<Features_IAuthenticationFeature>();
            var properties = authentication?.AuthenticationProperties;
            if (properties is not null)
            {
                var revoked = await RevokeIssuedAuthorizationAsync(
                    properties.ClientId,
                    properties.GetClaim<string>(Claims.Subject),
                    properties.AuthorizationId,
                    reason);
                if (revoked)
                {
                    context.Features.Set<Features_IAuthenticationFeature>(null);
                    context.Features.Set<IConnectionUserFeature>(null);
                }
            }

            await RevokePendingAuthorizationAsync(reason);
        }

        private async Task RevokePendingAuthorizationAsync(string reason)
        {
            var context = _contextAccessor.Context;
            var pendingAuthorization = context.Features.Get<PendingAuthorizationCleanup>();
            if (pendingAuthorization is null)
            {
                return;
            }

            var revoked = await RevokeIssuedAuthorizationAsync(
                pendingAuthorization.ClientId,
                pendingAuthorization.Subject,
                pendingAuthorization.AuthorizationId,
                reason);
            if (revoked)
            {
                context.Features.Set<PendingAuthorizationCleanup>(null);
            }
        }

        private async Task<bool> RevokeIssuedAuthorizationAsync(
            string? clientId,
            string? subject,
            string? authorizationId,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(authorizationId))
            {
                _logger.LogWarning(
                    "Cannot revoke the exact authorization after {Reason}: client, subject, or authorization id is missing.",
                    reason);
                return false;
            }

            try
            {
                var response = await _revokeTokenRequestClient.GetResponse<RevokeTokenResponseMessage>(
                    new RevokeTokenRequestMessage(clientId, subject, authorizationId),
                    CancellationToken.None);
                if (!response.Message.Succeeded)
                {
                    _logger.LogError(
                        "Failed to revoke exact authorization '{AuthorizationId}' after {Reason}: {Error}",
                        authorizationId,
                        reason,
                        response.Message.Error);
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to revoke exact authorization '{AuthorizationId}' after {Reason}.",
                    authorizationId,
                    reason);
                return false;
            }
        }

        public async Task SignOutAsync() =>
            await _authLogoutPipeline.ExecuteAsync(async (cancellationToken) =>
            {
                var context = _contextAccessor.Context;
                var masterId = context.GetMasterId();
                var character = context.GetCharacter();
                var session = context.GetSession();
                var persistenceSucceeded = character == null;
                if (character != null)
                {
                    _characterPersistenceService.TrackPendingLogout(character);
                }

                var sessionRemoved = session == null;
                try
                {
                    // Persist before removing the only registered copy. The EF bus outbox is
                    // the durable handoff boundary; consumer acknowledgement is asynchronous
                    // and is completed by the dehydration worker.
                    if (character != null)
                    {
                        await _characterPersistenceService.PersistAsync(character, force: true, cancellationToken: cancellationToken);
                        persistenceSucceeded = true;
                    }

                    if (session != null && persistenceSucceeded)
                    {
                        sessionRemoved = await _gameSessionService.RemoveSession(session);
                    }
                }
                finally
                {
                    if (character != null && persistenceSucceeded && sessionRemoved)
                    {
                        await _characterLogoutService.DetachAsync(character);
                    }
                }

                if (session is not null && session is not IGameWorldSession && masterId is not null)
                {
                    _mediator.Publish(new LobbySignOutCommand(masterId.Value, session.SessionGeneration, session.ConnectionId));
                }

                // Token revocation is remote cleanup. It must not retain a successfully
                // persisted live-session owner when the authorization service is slow or
                // unavailable. A later logout/reconnect cleanup can revoke the token again.
                await RevokeCurrentAuthenticationAsync("sign-out");
            });
    }
}
