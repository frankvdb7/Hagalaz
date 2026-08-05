using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Services;
using Nito.AsyncEx;

namespace Hagalaz.Services.GameWorld.Store;

/// <summary>
/// Stores local sessions while enforcing one account per process.
/// Distributed ownership is enforced by <see cref="Services.IGameSessionClaimStore"/>.
/// </summary>
public class GameSessionStore : IGameSessionStore, IGameSessionAbortStore
{
    private readonly AsyncReaderWriterLock _lock = new();
    private readonly Dictionary<string, SessionSlot> _slots = new();

    public async ValueTask<bool> TryAdd(IGameSession session)
    {
        using (await _lock.WriterLockAsync())
        {
            if (_slots.ContainsKey(session.ConnectionId) ||
                _slots.Values.Any(slot => slot.ActiveSession?.MasterId == session.MasterId ||
                                          slot.PendingWorld?.Session.MasterId == session.MasterId))
            {
                return false;
            }

            _slots.Add(session.ConnectionId, new SessionSlot { ActiveSession = session });
            return true;
        }
    }

    public async ValueTask<bool> TryReserveWorldSession(IGameWorldSession session)
    {
        using (await _lock.WriterLockAsync())
        {
            _slots.TryGetValue(session.ConnectionId, out var slot);
            if (slot?.PendingWorld != null ||
                slot?.PendingAbort != null ||
                slot?.ActiveSession != null && !slot.ActiveSession.MasterId.Equals(session.MasterId) ||
                _slots.Values.Any(existing => existing.PendingWorld?.CleanupRequested == true &&
                                              existing.PendingWorld.Session.MasterId == session.MasterId) ||
                _slots.Values.Any(existing => existing.ActiveSession is IGameWorldSession &&
                                              existing.ActiveSession.MasterId == session.MasterId))
            {
                return false;
            }

            var existingSession = FindActiveSessionByMasterIdUnsafe(session.MasterId);
            slot ??= GetOrCreateSlot(session.ConnectionId);
            slot.PendingWorld = new PendingWorldSession(session, existingSession);
            return true;
        }
    }

    public async ValueTask<(bool Committed, IGameSession? ReplacedSession)> TryCommitWorldSession(
        IGameWorldSession expectedSession)
    {
        using (await _lock.WriterLockAsync())
        {
            if (_slots.TryGetValue(expectedSession.ConnectionId, out var expectedSlot) &&
                ReferenceEquals(expectedSlot.ActiveSession, expectedSession))
            {
                return (true, null);
            }

            if (expectedSlot?.PendingWorld is not { } pendingSession ||
                !ReferenceEquals(pendingSession.Session, expectedSession) ||
                pendingSession.CleanupRequested)
            {
                return (false, null);
            }

            var currentSession = FindActiveSessionByMasterIdUnsafe(expectedSession.MasterId);
            if (currentSession != null && !ReferenceEquals(currentSession, pendingSession.PreviousSession))
            {
                return (false, null);
            }

            if (currentSession != null)
            {
                var currentSlot = _slots[currentSession.ConnectionId];
                if (currentSession.ConnectionId != expectedSession.ConnectionId && currentSlot.PendingAbort != null)
                {
                    return (false, null);
                }

                currentSlot.ActiveSession = null;
                if (currentSession.ConnectionId != expectedSession.ConnectionId)
                {
                    currentSlot.PendingAbort = new PendingSessionAbort(currentSession);
                }

                RemoveSlotIfEmpty(currentSession.ConnectionId, currentSlot);
            }

            expectedSlot.PendingWorld = null;
            expectedSlot.ActiveSession = expectedSession;
            return (true, currentSession);
        }
    }

    public async ValueTask<bool> TryRetainWorldSessionForCleanup(IGameSession expectedSession)
    {
        using (await _lock.WriterLockAsync())
        {
            if (!_slots.TryGetValue(expectedSession.ConnectionId, out var slot))
            {
                return false;
            }

            if (slot.PendingWorld is { } pendingSession &&
                ReferenceEquals(pendingSession.Session, expectedSession))
            {
                pendingSession.CleanupRequested = true;
                return true;
            }

            if (!ReferenceEquals(slot.ActiveSession, expectedSession) || expectedSession is not IGameWorldSession)
            {
                return false;
            }

            slot.ActiveSession = null;
            slot.PendingWorld = new PendingWorldSession(expectedSession, null) { CleanupRequested = true };
            return true;
        }
    }

    public async ValueTask<bool> TryRemovePendingWorldSession(IGameSession expectedSession)
    {
        using (await _lock.WriterLockAsync())
        {
            return TryRemovePendingWorldSessionUnsafe(expectedSession);
        }
    }

    public async ValueTask<bool> IsPendingWorldSession(IGameSession expectedSession)
    {
        using (await _lock.ReaderLockAsync())
        {
            return _slots.TryGetValue(expectedSession.ConnectionId, out var slot) &&
                   slot.PendingWorld is { } pendingSession &&
                   ReferenceEquals(pendingSession.Session, expectedSession);
        }
    }

    public async ValueTask<(bool Found, IGameSession? Session)> TryGetValue(string connectionId)
    {
        using (await _lock.ReaderLockAsync())
        {
            if (_slots.TryGetValue(connectionId, out var slot) && slot.ActiveSession != null)
            {
                return (true, slot.ActiveSession);
            }

            return (false, null);
        }
    }

    public async ValueTask<IReadOnlyList<IGameWorldSession>> FindWorldSessionsPendingCleanup()
    {
        using (await _lock.ReaderLockAsync())
        {
            return _slots.Values
                .Where(slot => slot.PendingWorld?.CleanupRequested == true)
                .Select(slot => (IGameWorldSession)slot.PendingWorld!.Session)
                .ToArray();
        }
    }

    public async ValueTask<bool> TryMoveToPendingAbort(IGameSession expectedSession)
    {
        using (await _lock.WriterLockAsync())
        {
            if (!_slots.TryGetValue(expectedSession.ConnectionId, out var slot) || slot.PendingAbort != null)
            {
                return false;
            }

            if (ReferenceEquals(slot.ActiveSession, expectedSession))
            {
                slot.ActiveSession = null;
                slot.PendingAbort = new PendingSessionAbort(expectedSession);
                return true;
            }

            if (slot.PendingWorld is { } pendingSession &&
                ReferenceEquals(pendingSession.Session, expectedSession))
            {
                slot.PendingWorld = null;
                slot.PendingAbort = new PendingSessionAbort(expectedSession);
                return true;
            }

            return false;
        }
    }

    public async ValueTask<bool> TryBeginPendingSessionAbort(IGameSession expectedSession)
    {
        using (await _lock.WriterLockAsync())
        {
            if (!_slots.TryGetValue(expectedSession.ConnectionId, out var slot) ||
                slot.PendingAbort is not { } pendingAbort ||
                !ReferenceEquals(pendingAbort.Session, expectedSession) ||
                pendingAbort.Processing && !IsProcessingLeaseExpired(pendingAbort))
            {
                return false;
            }

            pendingAbort.Processing = true;
            pendingAbort.ProcessingStartedAtUtc = DateTimeOffset.UtcNow;
            return true;
        }
    }

    public async ValueTask<bool> TryCompletePendingSessionAbort(IGameSession expectedSession)
    {
        using (await _lock.WriterLockAsync())
        {
            if (!_slots.TryGetValue(expectedSession.ConnectionId, out var slot) ||
                slot.PendingAbort is not { } pendingAbort ||
                !ReferenceEquals(pendingAbort.Session, expectedSession) ||
                !pendingAbort.Processing)
            {
                return false;
            }

            slot.PendingAbort = null;
            RemoveSlotIfEmpty(expectedSession.ConnectionId, slot);
            return true;
        }
    }

    public async ValueTask<bool> TryReleasePendingSessionAbort(IGameSession expectedSession)
    {
        using (await _lock.WriterLockAsync())
        {
            if (!_slots.TryGetValue(expectedSession.ConnectionId, out var slot) ||
                slot.PendingAbort is not { } pendingAbort ||
                !ReferenceEquals(pendingAbort.Session, expectedSession) ||
                !pendingAbort.Processing)
            {
                return false;
            }

            pendingAbort.Processing = false;
            pendingAbort.ProcessingStartedAtUtc = null;
            return true;
        }
    }

    public async ValueTask<IReadOnlyList<IGameSession>> FindSessionsPendingAbort()
    {
        using (await _lock.ReaderLockAsync())
        {
            return _slots.Values
                .Where(slot => slot.PendingAbort is { Processing: false } ||
                               slot.PendingAbort is { } pendingAbort && IsProcessingLeaseExpired(pendingAbort))
                .Select(slot => slot.PendingAbort!.Session)
                .ToArray();
        }
    }

    public async ValueTask<(bool Removed, IGameSession? Session)> TryRemove(IGameSession expectedSession)
    {
        using (await _lock.WriterLockAsync())
        {
            if (!_slots.TryGetValue(expectedSession.ConnectionId, out var slot))
            {
                return (false, null);
            }

            if (!ReferenceEquals(slot.ActiveSession, expectedSession))
            {
                return TryRemovePendingWorldSessionUnsafe(expectedSession)
                    ? (true, expectedSession)
                    : (false, null);
            }

            slot.ActiveSession = null;
            RemoveSlotIfEmpty(expectedSession.ConnectionId, slot);
            return (true, expectedSession);
        }
    }

    public async ValueTask<IGameSession?> FindByMasterId(uint masterId)
    {
        using (await _lock.ReaderLockAsync())
        {
            return FindActiveSessionByMasterIdUnsafe(masterId);
        }
    }

    public async ValueTask<IGameWorldSession?> FindWorldSessionByMasterId(uint masterId)
    {
        using (await _lock.ReaderLockAsync())
        {
            return _slots.Values
                .Select(slot => slot.ActiveSession)
                .OfType<IGameWorldSession>()
                .FirstOrDefault(session => session.MasterId == masterId);
        }
    }

    public async ValueTask<IReadOnlyList<IGameSession>> FindAll()
    {
        using (await _lock.ReaderLockAsync())
        {
            return _slots.Values
                .SelectMany(slot => new[] { slot.ActiveSession, slot.PendingWorld?.Session })
                .Where(session => session != null)
                .Cast<IGameSession>()
                .ToArray();
        }
    }

    private SessionSlot GetOrCreateSlot(string connectionId)
    {
        if (!_slots.TryGetValue(connectionId, out var slot))
        {
            slot = new SessionSlot();
            _slots.Add(connectionId, slot);
        }

        return slot;
    }

    private void RemoveSlotIfEmpty(string connectionId, SessionSlot slot)
    {
        if (slot.ActiveSession == null && slot.PendingWorld == null && slot.PendingAbort == null)
        {
            _slots.Remove(connectionId);
        }
    }

    private IGameSession? FindActiveSessionByMasterIdUnsafe(uint masterId) =>
        _slots.Values
            .Select(slot => slot.ActiveSession)
            .FirstOrDefault(session => session?.MasterId == masterId);

    private static bool IsProcessingLeaseExpired(PendingSessionAbort pendingAbort) =>
        pendingAbort.Processing &&
        pendingAbort.ProcessingStartedAtUtc is { } startedAtUtc &&
        startedAtUtc + GameSessionClaimOptions.LeaseDuration <= DateTimeOffset.UtcNow;

    private bool TryRemovePendingWorldSessionUnsafe(IGameSession expectedSession)
    {
        if (!_slots.TryGetValue(expectedSession.ConnectionId, out var slot) ||
            slot.PendingWorld is not { } pendingSession ||
            !ReferenceEquals(pendingSession.Session, expectedSession))
        {
            return false;
        }

        slot.PendingWorld = null;
        RemoveSlotIfEmpty(expectedSession.ConnectionId, slot);
        return true;
    }

    private sealed class SessionSlot
    {
        public IGameSession? ActiveSession { get; set; }
        public PendingWorldSession? PendingWorld { get; set; }
        public PendingSessionAbort? PendingAbort { get; set; }
    }

    private sealed class PendingWorldSession
    {
        public PendingWorldSession(IGameSession session, IGameSession? previousSession)
        {
            Session = session;
            PreviousSession = previousSession;
        }

        public IGameSession Session { get; }
        public IGameSession? PreviousSession { get; }
        public bool CleanupRequested { get; set; }
    }

    private sealed class PendingSessionAbort
    {
        public PendingSessionAbort(IGameSession session) => Session = session;

        public IGameSession Session { get; }
        public bool Processing { get; set; }
        public DateTimeOffset? ProcessingStartedAtUtc { get; set; }
    }
}
