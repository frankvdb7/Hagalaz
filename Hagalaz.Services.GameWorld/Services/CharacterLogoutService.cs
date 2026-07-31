using System;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Mediator;

namespace Hagalaz.Services.GameWorld.Services;

public interface ICharacterLogoutService
{
    Task DetachAsync(ICharacter character, CancellationToken cancellationToken = default);
    Task<bool> CompleteAsync(uint masterId, CancellationToken cancellationToken = default);
    Task<bool> AcknowledgeAndCompleteAsync(uint masterId, long snapshotRevision, CancellationToken cancellationToken = default);
}

public sealed class CharacterLogoutService : ICharacterLogoutService
{
    private readonly CharacterPersistenceState _state;
    private readonly ICharacterService _characterService;
    private readonly IGameMediator _mediator;

    public CharacterLogoutService(
        CharacterPersistenceState state,
        ICharacterService characterService,
        IGameMediator mediator)
    {
        _state = state;
        _characterService = characterService;
        _mediator = mediator;
    }

    public async Task DetachAsync(ICharacter character, CancellationToken cancellationToken = default)
    {
        if (!_state.TryGetPendingLogout(character.MasterId, out _))
        {
            return;
        }

        if (_state.IsPendingLogoutRemoved(character))
        {
            await CompleteAsync(character.MasterId, cancellationToken);
            return;
        }

        var removed = await _characterService.RemoveAsync(character);
        if (!removed)
        {
            throw new InvalidOperationException($"Failed to remove character '{character}' from the character store during sign out.");
        }

        _state.MarkPendingLogoutRemoved(character);
        await CompleteAsync(character.MasterId, cancellationToken);
    }

    public async Task<bool> AcknowledgeAndCompleteAsync(
        uint masterId,
        long snapshotRevision,
        CancellationToken cancellationToken = default)
    {
        _state.Acknowledge(masterId, snapshotRevision);
        return await CompleteAsync(masterId, cancellationToken);
    }

    public Task<bool> CompleteAsync(uint masterId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_state.IsPersistenceAcknowledged(masterId) ||
            !_state.TryGetPendingLogout(masterId, out var character) ||
            !_state.IsPendingLogoutRemoved(character))
        {
            return Task.FromResult(false);
        }

        if (!_state.TryBeginLogoutCompletion(masterId))
        {
            return Task.FromResult(false);
        }

        try
        {
            _state.Forget(masterId);
            if (!character.IsDestroyed)
            {
                character.Destroy();
            }

            _mediator.Publish(new WorldSignOutCommand(masterId));
        }
        finally
        {
            _state.EndLogoutCompletion(masterId);
        }
        return Task.FromResult(true);
    }
}
