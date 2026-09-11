using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Mediator;

namespace Hagalaz.Services.GameWorld.Services;

public interface ICharacterLogoutService
{
    void TrackPendingLogout(ICharacter character);
    bool IsPendingLogout(ICharacter character);
    IReadOnlyCollection<ICharacter> GetPendingLogouts();
    Task DetachAsync(ICharacter character, CancellationToken cancellationToken = default);
    Task<bool> CompleteAsync(uint masterId, CancellationToken cancellationToken = default);
    Task<bool> AcknowledgeAndCompleteAsync(
        uint masterId,
        Guid correlationId,
        long snapshotRevision,
        CancellationToken cancellationToken = default,
        CharacterPersistenceOutcome? outcome = null);
}

public sealed class CharacterLogoutState
{
    private readonly ConcurrentDictionary<uint, PendingLogout> _pending = new();

    public void Track(ICharacter character) => _pending[character.MasterId] = new PendingLogout(character);

    public bool IsPending(ICharacter character) =>
        _pending.TryGetValue(character.MasterId, out var pending) &&
        ReferenceEquals(pending.Character, character);

    public IReadOnlyCollection<ICharacter> GetPending() => _pending.Values.Select(value => value.Character).ToArray();

    public bool TryMarkRemoved(ICharacter character)
    {
        return _pending.TryGetValue(character.MasterId, out var pending)
            && ReferenceEquals(pending.Character, character)
            && Interlocked.Exchange(ref pending.Removed, 1) == 0;
    }

    public bool IsRemoved(ICharacter character) =>
        _pending.TryGetValue(character.MasterId, out var pending)
        && ReferenceEquals(pending.Character, character)
        && Volatile.Read(ref pending.Removed) != 0;

    public bool TryGet(uint masterId, out PendingLogout pending) => _pending.TryGetValue(masterId, out pending!);

    public bool TryBeginCompletion(PendingLogout pending) => Interlocked.CompareExchange(ref pending.Completing, 1, 0) == 0;

    public void EndCompletion(PendingLogout pending) => Volatile.Write(ref pending.Completing, 0);

    public void Remove(uint masterId, PendingLogout pending) =>
        ((ICollection<KeyValuePair<uint, PendingLogout>>)_pending).Remove(new KeyValuePair<uint, PendingLogout>(masterId, pending));

    public sealed class PendingLogout
    {
        public PendingLogout(ICharacter character) => Character = character;

        public ICharacter Character { get; }
        public int Removed;
        public int Completing;
    }
}

public sealed class CharacterLogoutService : ICharacterLogoutService
{
    private readonly CharacterPersistenceState _persistenceState;
    private readonly CharacterLogoutState _logoutState;
    private readonly ICharacterService _characterService;
    private readonly IGameMediator _mediator;

    public CharacterLogoutService(
        CharacterPersistenceState persistenceState,
        CharacterLogoutState logoutState,
        ICharacterService characterService,
        IGameMediator mediator)
    {
        _persistenceState = persistenceState;
        _logoutState = logoutState;
        _characterService = characterService;
        _mediator = mediator;
    }

    public void TrackPendingLogout(ICharacter character) => _logoutState.Track(character);

    public bool IsPendingLogout(ICharacter character) => _logoutState.IsPending(character);

    public IReadOnlyCollection<ICharacter> GetPendingLogouts() => _logoutState.GetPending();

    public async Task DetachAsync(ICharacter character, CancellationToken cancellationToken = default)
    {
        if (!_logoutState.IsPending(character))
        {
            return;
        }

        if (_logoutState.IsRemoved(character))
        {
            await CompleteAsync(character.MasterId, cancellationToken);
            return;
        }

        var removed = await _characterService.RemoveAsync(character);
        if (!removed)
        {
            throw new InvalidOperationException($"Failed to remove character '{character}' from the character store during sign out.");
        }

        _logoutState.TryMarkRemoved(character);
        await CompleteAsync(character.MasterId, cancellationToken);
    }

    public async Task<bool> AcknowledgeAndCompleteAsync(
        uint masterId,
        Guid correlationId,
        long snapshotRevision,
        CancellationToken cancellationToken = default,
        CharacterPersistenceOutcome? outcome = null)
    {
        if (outcome is CharacterPersistenceOutcome.Committed or CharacterPersistenceOutcome.Duplicate)
        {
            _persistenceState.Acknowledge(masterId, correlationId, snapshotRevision);
        }

        return await CompleteAsync(masterId, cancellationToken);
    }

    public Task<bool> CompleteAsync(uint masterId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_persistenceState.IsPersistenceAcknowledged(masterId) ||
            !_logoutState.TryGet(masterId, out var pending) ||
            !_logoutState.IsRemoved(pending.Character))
        {
            return Task.FromResult(false);
        }

        if (!_logoutState.TryBeginCompletion(pending))
        {
            return Task.FromResult(false);
        }

        try
        {
            var character = pending.Character;
            var connectionId = character.Session.ConnectionId;
            var sessionGeneration = character.Session.SessionGeneration;
            if (!character.IsDestroyed)
            {
                character.Destroy();
            }

            _mediator.Publish(new WorldSignOutCommand(masterId, sessionGeneration, connectionId));
            _persistenceState.Forget(masterId);
            _logoutState.Remove(masterId, pending);
        }
        finally
        {
            _logoutState.EndCompletion(pending);
        }
        return Task.FromResult(true);
    }
}
