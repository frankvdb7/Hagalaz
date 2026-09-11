using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Factories;
using Hagalaz.Services.GameWorld.Features;
using Hagalaz.Services.GameWorld.Logic.Characters.Messages;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Services.Model;
using MassTransit;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Logging;
using Raido.Server;
using Features_AuthenticationProperties = Hagalaz.Services.GameWorld.Features.AuthenticationProperties;

namespace Hagalaz.Services.GameWorld.Services;

/// <summary>
/// Owns the reserve, initialize, commit, and compensation transaction for world admission.
/// </summary>
public sealed class WorldSessionAdmissionService : IWorldSessionAdmissionService
{
    private readonly ILogger<WorldSessionAdmissionService> _logger;
    private readonly IMapper _mapper;
    private readonly ICharacterService _characterService;
    private readonly ICharacterFactory _characterFactory;
    private readonly ICharacterHydrationService _characterHydrationService;
    private readonly ICharacterPersistenceService _characterPersistenceService;
    private readonly IGameSessionService _gameSessionService;
    private readonly IRequestClient<HydrateCharacter> _getCharacterRequestClient;

    public WorldSessionAdmissionService(
        ILogger<WorldSessionAdmissionService> logger,
        IMapper mapper,
        ICharacterService characterService,
        ICharacterFactory characterFactory,
        ICharacterHydrationService characterHydrationService,
        ICharacterPersistenceService characterPersistenceService,
        IGameSessionService gameSessionService,
        IRequestClient<HydrateCharacter> getCharacterRequestClient)
    {
        _logger = logger;
        _mapper = mapper;
        _characterService = characterService;
        _characterFactory = characterFactory;
        _characterHydrationService = characterHydrationService;
        _characterPersistenceService = characterPersistenceService;
        _gameSessionService = gameSessionService;
        _getCharacterRequestClient = getCharacterRequestClient;
    }

    public async ValueTask<SignInResult> AdmitAsync(
        SignInRequest signInRequest,
        RaidoCallerContext context,
        uint masterId,
        Features_AuthenticationProperties authenticationProperties,
        CancellationToken cancellationToken = default)
    {
        (IGameSession? Session, bool Created) sessionRegistration = signInRequest.LobbySessionClaimId is null
            ? await _gameSessionService.TryAddWorldSession(masterId, context.ConnectionId, cancellationToken)
            : await _gameSessionService.TryAddWorldSession(
                masterId,
                context.ConnectionId,
                signInRequest.LobbySessionClaimId,
                cancellationToken);

        if (!sessionRegistration.Created || sessionRegistration.Session is null)
        {
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
                var response = await _getCharacterRequestClient.GetResponse<CharacterHydrated, CharacterNotFound>(
                    new HydrateCharacter(masterId), cancellationToken);
                if (response.Is<CharacterNotFound>(out _))
                {
                    return SignInResult.Fail;
                }

                if (response.Is<CharacterHydrated>(out var hydrated))
                {
                    characterModel = _mapper.Map<CharacterModel>(hydrated.Message) with
                    {
                        Claims = _mapper.Map<HydratedClaims>(authenticationProperties)
                    };
                }
                else
                {
                    _logger.LogError("Failed to get valid hydrate character response '{type}'", response.Message.GetType());
                    return SignInResult.Fail;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to get hydrate character response");
                return SignInResult.Fail;
            }

            var character = _characterFactory.Create(session, signInRequest.GameClient);
            registeredCharacter = character;
            if (!await _characterHydrationService.HydrateAsync(character, characterModel))
            {
                _logger.LogWarning("Unable to hydrate character '{character}'", character);
                return SignInResult.Fail;
            }

            _characterPersistenceService.InitializeRevision(masterId, characterModel.SnapshotRevision);
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

            context.Features.Set<ICharacterFeature>(new CharacterFeature { Character = character });
            context.Features.Set<ISessionFeature>(new SessionFeature { Session = session });
            context.Features.Set<IContactsFeature>(new WorldContactsFeature(character));
            context.Features.Set<IUserProfileFeature>(new UserProfileFeature());
            signInSucceeded = true;
            return SignInResult.Success;
        }
        finally
        {
            if (!signInSucceeded)
            {
                await RollbackAsync(
                    masterId,
                    sessionRegistration.Session,
                    registeredCharacter,
                    characterRegistered,
                    revisionInitialized);
            }
        }
    }

    private async Task RollbackAsync(
        uint masterId,
        IGameSession session,
        ICharacter? registeredCharacter,
        bool characterRegistered,
        bool revisionInitialized)
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
                            _characterPersistenceService.Forget(masterId);
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, "Failed to forget character persistence state after world sign-in failed");
                        }

                        try
                        {
                            registeredCharacter.Destroy();
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, "Failed to destroy character after world sign-in failed");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Character '{MasterId}' removal returned false after world sign-in failed; retaining persistence state for recovery", masterId);
                    }
                }
                catch (OperationCanceledException exception)
                {
                    _logger.LogError(exception, "Character removal was canceled after world sign-in failed");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to remove character after world sign-in failed");
                }
            }
            else
            {
                try
                {
                    registeredCharacter.Destroy();
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to destroy unregistered character after world sign-in failed");
                }
            }
        }

        if (!characterRegistered && revisionInitialized)
        {
            try
            {
                if (await _characterService.FindByMasterId(masterId) is null)
                {
                    _characterPersistenceService.Forget(masterId);
                }
                else
                {
                    _logger.LogWarning("Character '{MasterId}' was already registered after world sign-in failed; retaining persistence state for the existing character", masterId);
                }
            }
            catch (OperationCanceledException exception)
            {
                _logger.LogError(exception, "Unable to determine character registration after world sign-in failed; retaining persistence state");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to determine character registration after world sign-in failed; retaining persistence state");
            }
        }

        try
        {
            await _gameSessionService.RemoveSession(session, CancellationToken.None);
        }
        catch (OperationCanceledException exception)
        {
            _logger.LogError(exception, "Game-session removal was canceled after world sign-in failed");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to remove game session '{connectionId}' after world sign-in failed", session.ConnectionId);
        }
        finally
        {
            try
            {
                await _gameSessionService.RemoveLocalSession(session);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to remove local game session '{connectionId}' after world sign-in failed", session.ConnectionId);
            }
        }
    }
}
