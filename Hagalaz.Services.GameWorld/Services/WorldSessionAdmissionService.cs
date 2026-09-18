using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
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
/// Owns the reserve, initialize, commit, and compensation flow for world admission.
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
    private readonly IRsTaskService _taskScheduler;

    public WorldSessionAdmissionService(
        ILogger<WorldSessionAdmissionService> logger,
        IMapper mapper,
        ICharacterService characterService,
        ICharacterFactory characterFactory,
        ICharacterHydrationService characterHydrationService,
        ICharacterPersistenceService characterPersistenceService,
        IGameSessionService gameSessionService,
        IRequestClient<HydrateCharacter> getCharacterRequestClient,
        IRsTaskService taskScheduler)
    {
        _logger = logger;
        _mapper = mapper;
        _characterService = characterService;
        _characterFactory = characterFactory;
        _characterHydrationService = characterHydrationService;
        _characterPersistenceService = characterPersistenceService;
        _gameSessionService = gameSessionService;
        _getCharacterRequestClient = getCharacterRequestClient;
        _taskScheduler = taskScheduler;
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
        ICharacter? character = null;
        var characterRegistered = false;
        var persistenceInitialized = false;
        try
        {
            CharacterModel characterModel;
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
                throw new InvalidOperationException("Hydrate character request returned an unexpected response type.");
            }

            character = _characterFactory.Create(session, signInRequest.GameClient);
            if (!await _characterHydrationService.HydrateAsync(character, characterModel))
            {
                _logger.LogWarning("Unable to hydrate character '{character}'", character);
                return SignInResult.Fail;
            }

            _characterPersistenceService.InitializeRevision(
                masterId,
                characterModel.SnapshotRevision,
                session.SessionGeneration);
            persistenceInitialized = true;

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
                    character,
                    characterRegistered,
                    persistenceInitialized);
            }
        }
    }

    private void DestroyUnregisteredCharacter(ICharacter character)
    {
        try
        {
            character.Destroy();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to destroy unregistered character after world sign-in failed");
        }
    }

    private async Task RollbackAsync(
        uint masterId,
        IGameSession session,
        ICharacter? character,
        bool characterRegistered,
        bool persistenceInitialized)
    {
        if (character is not null && !characterRegistered)
        {
            DestroyUnregisteredCharacter(character);
            ReleasePersistenceState(masterId, session, persistenceInitialized);
        }

        if (character is not null && characterRegistered)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _taskScheduler.Schedule(new RsTask(() =>
            {
                var removed = false;
                try
                {
                    removed = _characterService.Remove(character);
                    if (removed)
                    {
                        DestroyUnregisteredCharacter(character);
                        ReleasePersistenceState(masterId, session, persistenceInitialized);
                    }
                    else
                    {
                        _logger.LogWarning("Character '{MasterId}' removal returned false after world sign-in failed; retaining persistence state for recovery", masterId);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Character '{MasterId}' removal failed after world sign-in failed; retaining persistence state for recovery",
                        masterId);
                }
                finally
                {
                    completion.TrySetResult(removed);
                }
            }, 1));

            if (!await completion.Task)
            {
                _logger.LogError(
                    "Retaining game session '{connectionId}' because character rollback did not remove the exact store owner",
                    session.ConnectionId);
                return;
            }
        }

        await _gameSessionService.RemoveSession(session, CancellationToken.None);
    }

    private void ReleasePersistenceState(uint masterId, IGameSession session, bool persistenceInitialized)
    {
        if (persistenceInitialized && !_characterPersistenceService.Release(masterId, session.SessionGeneration))
        {
            _logger.LogWarning(
                "Persistence state for character '{MasterId}' was not released because a different lifecycle owns it",
                masterId);
        }
    }
}
