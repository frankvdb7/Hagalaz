using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Messages.Mediator;

namespace Hagalaz.Services.GameWorld.Services;

public interface ICharacterLogoutService
{
    bool TryBeginLogout(
        ICharacter character,
        out bool created,
        out CharacterPersistenceReceipt? persistenceReceipt);

    bool SetPendingLogoutPersistence(ICharacter character, CharacterPersistenceReceipt receipt);
    void CancelPendingLogout(ICharacter character);
    bool IsPendingLogout(ICharacter character);
    Task DetachAsync(ICharacter character, CancellationToken cancellationToken = default);
}

public sealed class CharacterLogoutState
{
    private readonly object _gate = new();
    private readonly Dictionary<uint, PendingLogout> _pending = new();

    public bool TryBeginLogout(
        ICharacter character,
        out bool created,
        out CharacterPersistenceReceipt? persistenceReceipt)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending))
            {
                pending = new PendingLogout(character);
                _pending.Add(character.MasterId, pending);
                created = true;
                persistenceReceipt = null;
                return true;
            }

            created = false;
            persistenceReceipt = ReferenceEquals(pending.Character, character)
                ? pending.PersistenceReceipt
                : null;
            return ReferenceEquals(pending.Character, character);
        }
    }

    public bool SetPersistenceReceipt(ICharacter character, CharacterPersistenceReceipt receipt)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) ||
                !ReferenceEquals(pending.Character, character))
            {
                return false;
            }

            pending.PersistenceReceipt = receipt;
            return true;
        }
    }

    public bool IsPending(ICharacter character)
    {
        lock (_gate)
        {
            return _pending.TryGetValue(character.MasterId, out var pending) &&
                   ReferenceEquals(pending.Character, character);
        }
    }

    public bool IsRemoved(ICharacter character)
    {
        lock (_gate)
        {
            return _pending.TryGetValue(character.MasterId, out var pending) &&
                   ReferenceEquals(pending.Character, character) &&
                   pending.CharacterRemoved;
        }
    }

    public bool MarkRemoved(ICharacter character)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) ||
                !ReferenceEquals(pending.Character, character))
            {
                return false;
            }

            pending.CharacterRemoved = true;
            return true;
        }
    }

    public void Remove(ICharacter character)
    {
        lock (_gate)
        {
            if (_pending.TryGetValue(character.MasterId, out var pending) &&
                ReferenceEquals(pending.Character, character))
            {
                _pending.Remove(character.MasterId);
            }
        }
    }

    public sealed class PendingLogout
    {
        public PendingLogout(ICharacter character) => Character = character;

        public ICharacter Character { get; }
        public CharacterPersistenceReceipt? PersistenceReceipt { get; set; }
        public bool CharacterRemoved { get; set; }
    }
}

public sealed class CharacterLogoutService : ICharacterLogoutService
{
    private readonly CharacterLogoutState _logoutState;
    private readonly ICharacterService _characterService;
    private readonly IGameMediator _mediator;

    public CharacterLogoutService(
        CharacterLogoutState logoutState,
        ICharacterService characterService,
        IGameMediator mediator)
    {
        _logoutState = logoutState;
        _characterService = characterService;
        _mediator = mediator;
    }

    public bool TryBeginLogout(
        ICharacter character,
        out bool created,
        out CharacterPersistenceReceipt? persistenceReceipt) =>
        _logoutState.TryBeginLogout(character, out created, out persistenceReceipt);

    public bool SetPendingLogoutPersistence(ICharacter character, CharacterPersistenceReceipt receipt) =>
        _logoutState.SetPersistenceReceipt(character, receipt);

    public void CancelPendingLogout(ICharacter character) => _logoutState.Remove(character);

    public bool IsPendingLogout(ICharacter character) => _logoutState.IsPending(character);

    public async Task DetachAsync(ICharacter character, CancellationToken cancellationToken = default)
    {
        if (!_logoutState.IsPending(character))
        {
            return;
        }

        if (!_logoutState.IsRemoved(character))
        {
            var removed = await _characterService.RemoveAsync(character);
            if (!removed)
            {
                throw new InvalidOperationException($"Failed to remove character '{character}' from the character store during sign out.");
            }

            _logoutState.MarkRemoved(character);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!character.IsDestroyed)
        {
            character.Destroy();
        }

        var session = character.Session;
        _mediator.Publish(new WorldSignOutCommand(
            character.MasterId,
            session.SessionGeneration,
            session.ConnectionId));
        _logoutState.Remove(character);
    }
}
