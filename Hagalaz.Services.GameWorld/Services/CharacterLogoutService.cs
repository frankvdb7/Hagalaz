using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Services;

public interface ICharacterLogoutService
{
    bool TryBeginLogout(
        ICharacter character,
        out bool created,
        out CharacterPersistenceReceipt? persistenceReceipt);

    bool SetPendingLogoutPersistence(ICharacter character, CharacterPersistenceReceipt receipt);
    bool TryGetPendingPersistence(ICharacter character, out CharacterPersistenceReceipt? persistenceReceipt);
    bool TryBeginPersistenceSubmission(
        ICharacter character,
        out Task<CharacterPersistenceReceipt> completion,
        out bool shouldSubmit);
    bool FailPersistenceSubmission(ICharacter character, Exception exception);
    bool IsPendingLogout(ICharacter character);
    bool IsPendingLogout(uint masterId);
    Task<CharacterModel> DetachAsync(ICharacter character, CancellationToken cancellationToken = default);
    void CompleteLogout(ICharacter character);
}

public sealed class CharacterLogoutState
{
    private readonly object _gate = new();
    private readonly Dictionary<uint, PendingLogout> _pending = new();

    public bool TryBeginLogout(ICharacter character, out bool created, out CharacterPersistenceReceipt? persistenceReceipt)
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
            if (!ReferenceEquals(pending.Character, character))
            {
                persistenceReceipt = null;
                return false;
            }

            persistenceReceipt = pending.PersistenceReceipt;
            return true;
        }
    }

    public bool SetPersistenceReceipt(ICharacter character, CharacterPersistenceReceipt receipt)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) || !ReferenceEquals(pending.Character, character))
            {
                return false;
            }

            pending.PersistenceReceipt = receipt;
            pending.PersistenceSubmission?.TrySetResult(receipt);
            return true;
        }
    }

    public bool TryBeginPersistenceSubmission(
        ICharacter character,
        out Task<CharacterPersistenceReceipt> completion,
        out bool shouldSubmit)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) ||
                !ReferenceEquals(pending.Character, character))
            {
                completion = null!;
                shouldSubmit = false;
                return false;
            }

            if (pending.PersistenceReceipt is { } receipt)
            {
                completion = Task.FromResult(receipt);
                shouldSubmit = false;
                return true;
            }

            if (pending.PersistenceSubmission is { } existing)
            {
                completion = existing.Task;
                shouldSubmit = false;
                return true;
            }

            var submission = new TaskCompletionSource<CharacterPersistenceReceipt>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            pending.PersistenceSubmission = submission;
            completion = submission.Task;
            shouldSubmit = true;
            return true;
        }
    }

    public bool FailPersistenceSubmission(ICharacter character, Exception exception)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) ||
                !ReferenceEquals(pending.Character, character) ||
                pending.PersistenceSubmission is not { } submission)
            {
                return false;
            }

            pending.PersistenceSubmission = null;
            return submission.TrySetException(exception);
        }
    }

    public bool TryGetPersistenceReceipt(ICharacter character, out CharacterPersistenceReceipt? receipt)
    {
        lock (_gate)
        {
            if (_pending.TryGetValue(character.MasterId, out var pending) &&
                ReferenceEquals(pending.Character, character) && pending.PersistenceReceipt is not null)
            {
                receipt = pending.PersistenceReceipt;
                return true;
            }

            receipt = null;
            return false;
        }
    }

    public bool IsPending(ICharacter character) => IsPending(character.MasterId);

    public bool IsPending(uint masterId)
    {
        lock (_gate)
        {
            return _pending.ContainsKey(masterId);
        }
    }

    public bool TryGetSnapshot(ICharacter character, out CharacterModel snapshot)
    {
        lock (_gate)
        {
            if (_pending.TryGetValue(character.MasterId, out var pending) &&
                ReferenceEquals(pending.Character, character) && pending.Snapshot is not null)
            {
                snapshot = pending.Snapshot;
                return true;
            }

            snapshot = null!;
            return false;
        }
    }

    public bool SetSnapshot(ICharacter character, CharacterModel snapshot)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) ||
                !ReferenceEquals(pending.Character, character) || pending.Snapshot is not null)
            {
                return false;
            }

            pending.Snapshot = snapshot;
            return true;
        }
    }

    public bool TryBeginTerminalTransition(
        ICharacter character,
        out TaskCompletionSource<CharacterModel> completion,
        out bool shouldSchedule)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) || !ReferenceEquals(pending.Character, character))
            {
                completion = null!;
                shouldSchedule = false;
                return false;
            }

            if (pending.TerminalTransition is { } existing && !existing.Task.IsFaulted)
            {
                completion = existing;
                shouldSchedule = false;
                return true;
            }

            completion = new TaskCompletionSource<CharacterModel>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending.TerminalTransition = completion;
            shouldSchedule = true;
            return true;
        }
    }

    public bool TryComplete(ICharacter character, out WorldSignOutCommand command)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) ||
                !ReferenceEquals(pending.Character, character))
            {
                command = null!;
                return false;
            }

            command = new WorldSignOutCommand(pending.MasterId, pending.SessionGeneration, pending.ConnectionId);
            _pending.Remove(character.MasterId);
            return true;
        }
    }

    public void Remove(ICharacter character)
    {
        lock (_gate)
        {
            if (_pending.TryGetValue(character.MasterId, out var pending) && ReferenceEquals(pending.Character, character))
            {
                _pending.Remove(character.MasterId);
            }
        }
    }

    public sealed class PendingLogout
    {
        public PendingLogout(ICharacter character)
        {
            Character = character;
            MasterId = character.MasterId;
            SessionGeneration = character.Session.SessionGeneration;
            ConnectionId = character.Session.ConnectionId;
        }

        public ICharacter Character { get; }
        public uint MasterId { get; }
        public long SessionGeneration { get; }
        public string ConnectionId { get; }
        public CharacterModel? Snapshot { get; set; }
        public CharacterPersistenceReceipt? PersistenceReceipt { get; set; }
        public TaskCompletionSource<CharacterPersistenceReceipt>? PersistenceSubmission { get; set; }
        public TaskCompletionSource<CharacterModel>? TerminalTransition { get; set; }
    }
}

public sealed class CharacterLogoutService : ICharacterLogoutService
{
    private readonly CharacterLogoutState _logoutState;
    private readonly ICharacterStore _characterStore;
    private readonly IRsTaskService _taskService;
    private readonly IGameMediator _mediator;
    private readonly CharacterPersistenceState _persistenceState;

    public CharacterLogoutService(
        CharacterLogoutState logoutState,
        ICharacterStore characterStore,
        IRsTaskService taskService,
        IGameMediator mediator,
        CharacterPersistenceState persistenceState)
    {
        _logoutState = logoutState;
        _characterStore = characterStore;
        _taskService = taskService;
        _mediator = mediator;
        _persistenceState = persistenceState;
    }

    public bool TryBeginLogout(ICharacter character, out bool created, out CharacterPersistenceReceipt? persistenceReceipt) =>
        _logoutState.TryBeginLogout(character, out created, out persistenceReceipt);

    public bool SetPendingLogoutPersistence(ICharacter character, CharacterPersistenceReceipt receipt) =>
        _logoutState.SetPersistenceReceipt(character, receipt);

    public bool TryGetPendingPersistence(ICharacter character, out CharacterPersistenceReceipt? persistenceReceipt) =>
        _logoutState.TryGetPersistenceReceipt(character, out persistenceReceipt);

    public bool TryBeginPersistenceSubmission(
        ICharacter character,
        out Task<CharacterPersistenceReceipt> completion,
        out bool shouldSubmit) =>
        _logoutState.TryBeginPersistenceSubmission(character, out completion, out shouldSubmit);

    public bool FailPersistenceSubmission(ICharacter character, Exception exception) =>
        _logoutState.FailPersistenceSubmission(character, exception);

    public bool IsPendingLogout(ICharacter character) => _logoutState.IsPending(character);

    public bool IsPendingLogout(uint masterId) => _logoutState.IsPending(masterId);

    public async Task<CharacterModel> DetachAsync(ICharacter character, CancellationToken cancellationToken = default)
    {
        if (_logoutState.TryGetSnapshot(character, out var snapshot))
        {
            return snapshot;
        }

        if (!_logoutState.TryBeginTerminalTransition(character, out var completion, out var shouldSchedule))
        {
            throw new InvalidOperationException($"Character '{character.MasterId}' is not pending logout.");
        }

        if (shouldSchedule)
        {
            _taskService.Schedule(new RsTask(() =>
            {
                try
                {
                    var dehydrationService = character.ServiceProvider.GetRequiredService<ICharacterDehydrationService>();
                    var finalSnapshot = dehydrationService.Dehydrate(character) with
                    {
                        SnapshotRevision = _persistenceState.NextRevision(character.MasterId)
                    };

                    if (!_characterStore.Remove(character))
                    {
                        throw new InvalidOperationException(
                            $"Character '{character.MasterId}' was no longer owned by the character store during logout.");
                    }

                    if (!_logoutState.SetSnapshot(character, finalSnapshot))
                    {
                        throw new InvalidOperationException($"Character '{character.MasterId}' logout snapshot was already captured.");
                    }

                    character.Destroy();
                    completion.TrySetResult(finalSnapshot);
                }
                catch (Exception exception)
                {
                    completion.TrySetException(exception);
                    throw;
                }
            }, 1));
        }

        return await completion.Task.WaitAsync(cancellationToken);
    }

    public void CompleteLogout(ICharacter character)
    {
        if (_logoutState.TryComplete(character, out var command))
        {
            _mediator.Publish(command);
        }
    }
}
