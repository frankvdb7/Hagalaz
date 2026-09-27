using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Characters.Messages;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Messages.Mediator;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Services;

public interface ICharacterLogoutService
{
    bool TryBeginLogout(ICharacter character, out CharacterPersistenceReceipt? persistenceReceipt);

    bool SetPendingLogoutPersistence(ICharacter character, CharacterPersistenceReceipt receipt);
    bool TryGetPendingPersistence(ICharacter character, out CharacterPersistenceReceipt? persistenceReceipt);
    bool MarkSessionRemoved(ICharacter character);
    bool MarkRecoveryEligible(ICharacter character);
    bool IsPendingLogout(ICharacter character);
    bool IsPendingLogout(uint masterId);
    Task<CharacterModel> DetachAsync(ICharacter character, CancellationToken cancellationToken = default);
    Task RecoverPendingLogoutsAsync(CancellationToken cancellationToken = default);
    void CompleteLogout(ICharacter character);
}

public sealed class CharacterLogoutState
{
    internal enum ContinuationOwner
    {
        Normal,
        NormalClaimed,
        RecoveryAvailable,
        RecoveryClaimed
    }

    internal sealed class TerminalTransition
    {
        public TaskCompletionSource<CharacterModel> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool RecoveryRequested { get; set; }
    }

    private readonly object _gate = new();
    private readonly Dictionary<uint, PendingLogout> _pending = new();

    public bool TryBeginLogout(ICharacter character, out CharacterPersistenceReceipt? persistenceReceipt)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending))
            {
                pending = new PendingLogout(character);
                _pending.Add(character.MasterId, pending);
                persistenceReceipt = null;
                return true;
            }

            if (!ReferenceEquals(pending.Character, character) ||
                pending.ContinuationOwner is ContinuationOwner.RecoveryAvailable or ContinuationOwner.RecoveryClaimed)
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

            pending.PersistenceReceipt ??= receipt;
            return true;
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

    public bool MarkSessionRemoved(ICharacter character)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) ||
                !ReferenceEquals(pending.Character, character))
            {
                return false;
            }

            pending.SessionRemoved = true;
            return true;
        }
    }

    public bool ClearPersistenceReceipt(ICharacter character, CharacterPersistenceReceipt receipt)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) ||
                !ReferenceEquals(pending.Character, character) ||
                !ReferenceEquals(pending.PersistenceReceipt, receipt))
            {
                return false;
            }

            pending.PersistenceReceipt = null;
            return true;
        }
    }

    public bool MarkRecoveryEligible(ICharacter character)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) ||
                !ReferenceEquals(pending.Character, character) ||
                pending.Snapshot is null)
            {
                return false;
            }

            if (pending.ContinuationOwner == ContinuationOwner.RecoveryClaimed)
            {
                return true;
            }

            pending.ContinuationOwner = ContinuationOwner.RecoveryAvailable;
            return true;
        }
    }

    public bool MarkTerminalTransitionCanceled(
        ICharacter character,
        TaskCompletionSource<CharacterModel> completion)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) ||
                !ReferenceEquals(pending.Character, character) ||
                pending.TerminalTransition is not { } transition ||
                !ReferenceEquals(transition.Completion, completion))
            {
                return false;
            }

            if (pending.ContinuationOwner is ContinuationOwner.NormalClaimed or
                ContinuationOwner.RecoveryAvailable or
                ContinuationOwner.RecoveryClaimed)
            {
                return true;
            }

            transition.RecoveryRequested = true;
            if (pending.Snapshot is not null)
            {
                pending.ContinuationOwner = ContinuationOwner.RecoveryAvailable;
            }

            return true;
        }
    }

    public IReadOnlyList<PendingLogout> FindRecoverablePendingLogouts()
    {
        lock (_gate)
        {
            return _pending.Values
                .Where(pending => pending.Snapshot is not null &&
                                  pending.ContinuationOwner == ContinuationOwner.RecoveryAvailable)
                .ToArray();
        }
    }

    public bool TryClaimNormalContinuation(ICharacter character, CharacterModel snapshot)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) ||
                !ReferenceEquals(pending.Character, character) ||
                !ReferenceEquals(pending.Snapshot, snapshot) ||
                pending.ContinuationOwner != ContinuationOwner.Normal)
            {
                return false;
            }

            pending.ContinuationOwner = ContinuationOwner.NormalClaimed;
            return true;
        }
    }

    public bool TryClaimRecovery(PendingLogout pending)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(pending.MasterId, out var current) ||
                !ReferenceEquals(current, pending) ||
                current.Snapshot is null ||
                current.ContinuationOwner != ContinuationOwner.RecoveryAvailable)
            {
                return false;
            }

            current.ContinuationOwner = ContinuationOwner.RecoveryClaimed;
            return true;
        }
    }

    public void ReleaseRecoveryClaim(PendingLogout pending)
    {
        lock (_gate)
        {
            if (_pending.TryGetValue(pending.MasterId, out var current) &&
                ReferenceEquals(current, pending) &&
                current.ContinuationOwner == ContinuationOwner.RecoveryClaimed)
            {
                current.ContinuationOwner = ContinuationOwner.RecoveryAvailable;
            }
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

    public bool TryGetSessionGeneration(ICharacter character, out long sessionGeneration)
    {
        lock (_gate)
        {
            if (_pending.TryGetValue(character.MasterId, out var pending) &&
                ReferenceEquals(pending.Character, character))
            {
                sessionGeneration = pending.SessionGeneration;
                return true;
            }

            sessionGeneration = 0;
            return false;
        }
    }

    public bool SetSnapshot(ICharacter character, CharacterModel snapshot) =>
        SetSnapshot(character, snapshot, completion: null);

    public bool SetSnapshot(
        ICharacter character,
        CharacterModel snapshot,
        TaskCompletionSource<CharacterModel>? completion)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(character.MasterId, out var pending) ||
                !ReferenceEquals(pending.Character, character) ||
                pending.Snapshot is not null ||
                completion is not null && !IsCurrentTransition(pending, completion))
            {
                return false;
            }

            pending.Snapshot = snapshot;
            if (pending.TerminalTransition?.RecoveryRequested == true &&
                pending.ContinuationOwner == ContinuationOwner.Normal)
            {
                pending.ContinuationOwner = ContinuationOwner.RecoveryAvailable;
            }

            return true;
        }
    }

    private static bool IsCurrentTransition(
        PendingLogout pending,
        TaskCompletionSource<CharacterModel> completion) =>
        pending.TerminalTransition is { } transition &&
        ReferenceEquals(transition.Completion, completion);

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

            if (pending.TerminalTransition is { } existing && !existing.Completion.Task.IsFaulted)
            {
                completion = existing.Completion;
                shouldSchedule = false;
                return true;
            }

            var transition = new TerminalTransition();
            pending.TerminalTransition = transition;
            completion = transition.Completion;
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
            Session = character.Session;
            MasterId = character.MasterId;
            SessionGeneration = character.Session.SessionGeneration;
            ConnectionId = character.Session.ConnectionId;
        }

        public ICharacter Character { get; }
        public IGameSession Session { get; }
        public uint MasterId { get; }
        public long SessionGeneration { get; }
        public string ConnectionId { get; }
        public CharacterModel? Snapshot { get; set; }
        public CharacterPersistenceReceipt? PersistenceReceipt { get; set; }
        public bool SessionRemoved { get; set; }
        internal ContinuationOwner ContinuationOwner { get; set; }
        internal TerminalTransition? TerminalTransition { get; set; }
    }
}

public sealed class CharacterLogoutService : ICharacterLogoutService
{
    private readonly CharacterLogoutState _logoutState;
    private readonly ICharacterService _characterService;
    private readonly IRsTaskService _taskService;
    private readonly IGameMediator _mediator;
    private readonly CharacterPersistenceState _persistenceState;
    private readonly ICharacterPersistenceService? _persistenceService;
    private readonly IGameSessionService? _gameSessionService;
    private readonly ILogger<CharacterLogoutService> _logger;

    public CharacterLogoutService(
        CharacterLogoutState logoutState,
        ICharacterService characterService,
        IRsTaskService taskService,
        IGameMediator mediator,
        CharacterPersistenceState persistenceState,
        ICharacterPersistenceService? persistenceService = null,
        IGameSessionService? gameSessionService = null,
        ILogger<CharacterLogoutService>? logger = null)
    {
        _logoutState = logoutState;
        _characterService = characterService;
        _taskService = taskService;
        _mediator = mediator;
        _persistenceState = persistenceState;
        _persistenceService = persistenceService;
        _gameSessionService = gameSessionService;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CharacterLogoutService>.Instance;
    }

    public bool TryBeginLogout(ICharacter character, out CharacterPersistenceReceipt? persistenceReceipt) =>
        _logoutState.TryBeginLogout(character, out persistenceReceipt);

    public bool SetPendingLogoutPersistence(ICharacter character, CharacterPersistenceReceipt receipt) =>
        _logoutState.SetPersistenceReceipt(character, receipt);

    public bool TryGetPendingPersistence(ICharacter character, out CharacterPersistenceReceipt? persistenceReceipt) =>
        _logoutState.TryGetPersistenceReceipt(character, out persistenceReceipt);

    public bool MarkSessionRemoved(ICharacter character) => _logoutState.MarkSessionRemoved(character);

    public bool MarkRecoveryEligible(ICharacter character) => _logoutState.MarkRecoveryEligible(character);

    public bool IsPendingLogout(ICharacter character) => _logoutState.IsPending(character);

    public bool IsPendingLogout(uint masterId) => _logoutState.IsPending(masterId);

    public async Task RecoverPendingLogoutsAsync(CancellationToken cancellationToken = default)
    {
        if (_persistenceService is null || _gameSessionService is null)
        {
            throw new InvalidOperationException("Logout recovery requires persistence and session services.");
        }

        foreach (var pending in _logoutState.FindRecoverablePendingLogouts())
        {
            if (!_logoutState.TryClaimRecovery(pending))
            {
                continue;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await RecoverPendingLogoutAsync(pending, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to recover logout for character '{MasterId}'.", pending.MasterId);
            }
            finally
            {
                _logoutState.ReleaseRecoveryClaim(pending);
            }
        }
    }

    private async Task RecoverPendingLogoutAsync(
        CharacterLogoutState.PendingLogout pending,
        CancellationToken cancellationToken)
    {
        var receipt = pending.PersistenceReceipt;
        if (receipt is { IsCompleted: true })
        {
            var completedOutcome = await _persistenceService!.WaitForAcknowledgementAsync(receipt, cancellationToken);
            if (completedOutcome is not (CharacterPersistenceOutcome.Committed or CharacterPersistenceOutcome.Duplicate))
            {
                _logoutState.ClearPersistenceReceipt(pending.Character, receipt);
                receipt = null;
            }
        }

        if (receipt is null)
        {
            receipt = await _persistenceService!.PersistAsync(
                pending.MasterId,
                pending.Snapshot!,
                force: true,
                cancellationToken);
            if (receipt is null || !_logoutState.SetPersistenceReceipt(pending.Character, receipt))
            {
                throw new InvalidOperationException(
                    $"Final character persistence did not return an owned receipt for master id {pending.MasterId}.");
            }
        }

        var outcome = await _persistenceService.WaitForAcknowledgementAsync(receipt, cancellationToken);
        if (outcome is not (CharacterPersistenceOutcome.Committed or CharacterPersistenceOutcome.Duplicate))
        {
            _logoutState.ClearPersistenceReceipt(pending.Character, receipt);
            return;
        }

        if (!pending.SessionRemoved)
        {
            if (!await _gameSessionService!.RemoveSession(pending.Session, CancellationToken.None))
            {
                return;
            }

            _logoutState.MarkSessionRemoved(pending.Character);
        }

        CompleteLogout(pending.Character);
    }

    public async Task<CharacterModel> DetachAsync(ICharacter character, CancellationToken cancellationToken = default)
    {
        if (_logoutState.TryGetSnapshot(character, out var snapshot))
        {
            if (!_logoutState.TryClaimNormalContinuation(character, snapshot))
            {
                throw new InvalidOperationException(
                    $"Character '{character.MasterId}' logout continuation is owned by recovery or another caller.");
            }

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
                var snapshotEstablished = false;
                try
                {
                    var dehydrationService = character.ServiceProvider.GetRequiredService<ICharacterDehydrationService>();
                    var finalSnapshot = dehydrationService.Dehydrate(character) with
                    {
                        SnapshotRevision = _persistenceState.NextRevision(character.MasterId)
                    };

                    if (!_characterService.Remove(character))
                    {
                        throw new InvalidOperationException(
                            $"Character '{character.MasterId}' was no longer owned by the character store during logout.");
                    }

                    if (!_logoutState.SetSnapshot(character, finalSnapshot, completion))
                    {
                        throw new InvalidOperationException($"Character '{character.MasterId}' logout snapshot was already captured.");
                    }

                    snapshotEstablished = true;
                    character.Destroy();
                    completion.TrySetResult(finalSnapshot);
                }
                catch (Exception exception)
                {
                    if (snapshotEstablished)
                    {
                        _logoutState.MarkRecoveryEligible(character);
                    }

                    completion.TrySetException(exception);
                    throw;
                }
            }, 1));
        }

        try
        {
            var finalSnapshot = await completion.Task.WaitAsync(cancellationToken);
            if (!_logoutState.TryClaimNormalContinuation(character, finalSnapshot))
            {
                throw new InvalidOperationException(
                    $"Character '{character.MasterId}' logout continuation is owned by recovery or another caller.");
            }

            return finalSnapshot;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logoutState.MarkTerminalTransitionCanceled(character, completion);
            throw;
        }
    }

    public void CompleteLogout(ICharacter character)
    {
        if (!_logoutState.TryGetSessionGeneration(character, out var sessionGeneration))
        {
            return;
        }

        var releaseResult = _persistenceState.ReleaseForLogout(character.MasterId, sessionGeneration);
        if (releaseResult is CharacterPersistenceState.LogoutPersistenceReleaseResult.PendingPersistence)
        {
            _logger.LogError(
                "Could not release persistence state for character '{MasterId}' during logout completion.",
                character.MasterId);
            _logoutState.MarkRecoveryEligible(character);
            return;
        }

        if (_logoutState.TryComplete(character, out var command))
        {
            _mediator.Publish(command);
        }
    }
}
