using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    internal sealed class ContactPresenceState
    {
        private readonly object _gate = new();
        private readonly Dictionary<uint, PresenceEntry> _entries = new();
        private readonly Dictionary<uint, PendingPresenceObservation> _pendingObservations = new();
        private readonly TaskCompletionSource _initialSnapshot = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private long _observationSequence;
        private int _observationWindows;

        public Task WaitForInitialSnapshotAsync(CancellationToken cancellationToken) =>
            _initialSnapshot.Task.WaitAsync(cancellationToken);

        public long BeginObservationWindow()
        {
            lock (_gate)
            {
                _observationWindows++;
                return _observationSequence;
            }
        }

        public void EndObservationWindow(IContactList<Friend> friends)
        {
            lock (_gate)
            {
                if (_observationWindows == 0)
                {
                    return;
                }

                _observationWindows--;
                if (_observationWindows != 0)
                {
                    return;
                }

                foreach (var masterId in new List<uint>(_pendingObservations.Keys))
                {
                    if (friends.Get(masterId) is null)
                    {
                        _pendingObservations.Remove(masterId);
                    }
                }
            }
        }

        public long CaptureObservationBoundary()
        {
            lock (_gate)
            {
                return _observationSequence;
            }
        }

        public void ReplaceFriends(
            IContactList<Friend> friends,
            IEnumerable<Friend> replacement,
            IEnumerable<ContactPresenceOwner> owners,
            long observationBoundary)
        {
            lock (_gate)
            {
                friends.Set(replacement);

                var seededOwners = new HashSet<uint>();
                foreach (var owner in owners)
                {
                    seededOwners.Add(owner.MasterId);
                    var entry = GetOrCreateEntry(owner.MasterId);
                    if (!entry.HasGeneration || owner.SessionGeneration > entry.LatestSessionGeneration)
                    {
                        entry.LatestSessionGeneration = owner.SessionGeneration;
                        entry.HasGeneration = true;
                        entry.CurrentOwner = new SessionIdentity(owner.SessionGeneration, owner.ConnectionId);
                        entry.LatestObservationSequence = Math.Max(
                            entry.LatestObservationSequence,
                            observationBoundary);
                    }
                }

                foreach (var pending in new List<PendingPresenceObservation>(_pendingObservations.Values))
                {
                    if (friends.Get(pending.MasterId) is not null &&
                        pending.ObservationSequence > observationBoundary)
                    {
                        ApplyPendingObservation(pending);
                        _pendingObservations.Remove(pending.MasterId);
                    }
                }

                foreach (var entry in _entries)
                {
                    if (!seededOwners.Contains(entry.Key) &&
                        entry.Value.LatestObservationSequence <= observationBoundary)
                    {
                        entry.Value.CurrentOwner = null;
                        entry.Value.LatestObservationSequence = Math.Max(
                            entry.Value.LatestObservationSequence,
                            observationBoundary);
                    }
                }

                _initialSnapshot.TrySetResult();
            }
        }

        public Friend AddFriend(
            IContactList<Friend> friends,
            Friend friend,
            ContactPresenceOwner? owner,
            long observationBoundary)
        {
            lock (_gate)
            {
                var masterId = unchecked((uint)friend.MasterId);
                friends.Remove(masterId);
                friends.Add(friend);

                if (_pendingObservations.TryGetValue(masterId, out var pending) &&
                    pending.ObservationSequence > observationBoundary)
                {
                    ApplyPendingObservation(pending);
                    _pendingObservations.Remove(masterId);
                    return friend;
                }

                if (owner is { } presenceOwner)
                {
                    var entry = GetOrCreateEntry(masterId);
                    if (entry.LatestObservationSequence <= observationBoundary &&
                        (!entry.HasGeneration || presenceOwner.SessionGeneration > entry.LatestSessionGeneration))
                    {
                        entry.LatestSessionGeneration = presenceOwner.SessionGeneration;
                        entry.HasGeneration = true;
                        entry.CurrentOwner = new SessionIdentity(
                            presenceOwner.SessionGeneration,
                            presenceOwner.ConnectionId);
                        entry.LatestObservationSequence = Math.Max(
                            entry.LatestObservationSequence,
                            observationBoundary);
                    }
                }
                else if (_entries.TryGetValue(masterId, out var entry) &&
                         entry.LatestObservationSequence <= observationBoundary)
                {
                    entry.CurrentOwner = null;
                    entry.LatestObservationSequence = Math.Max(
                        entry.LatestObservationSequence,
                        observationBoundary);
                }

                return friend;
            }
        }

        public bool RemoveFriend(IContactList<Friend> friends, uint masterId)
        {
            lock (_gate)
            {
                var removed = friends.Get(masterId) is not null;
                friends.Remove(masterId);
                var ownerRemoved = _entries.Remove(masterId);
                return removed || ownerRemoved;
            }
        }

        public Friend? TrySignIn(
            uint masterId,
            long sessionGeneration,
            string connectionId,
            IContactList<Friend> friends)
        {
            lock (_gate)
            {
                if (_entries.TryGetValue(masterId, out var existingEntry) &&
                    existingEntry.HasGeneration &&
                    sessionGeneration <= existingEntry.LatestSessionGeneration)
                {
                    return null;
                }

                var friend = friends.Get(masterId);
                if (friend is null)
                {
                    if (_observationWindows != 0)
                    {
                        RecordPendingObservation(masterId, sessionGeneration, connectionId, signedIn: true);
                    }
                    return null;
                }

                _pendingObservations.Remove(masterId);
                var entry = existingEntry ?? GetOrCreateEntry(masterId);
                entry.LatestSessionGeneration = sessionGeneration;
                entry.HasGeneration = true;
                entry.CurrentOwner = new SessionIdentity(sessionGeneration, connectionId);
                entry.LatestObservationSequence = checked(++_observationSequence);
                return friend;
            }
        }

        public Friend? TrySignOut(
            uint masterId,
            long sessionGeneration,
            string connectionId,
            IContactList<Friend> friends)
        {
            lock (_gate)
            {
                var friend = friends.Get(masterId);
                if (friend is null)
                {
                    if (_observationWindows != 0)
                    {
                        RecordPendingObservation(masterId, sessionGeneration, connectionId, signedIn: false);
                    }
                    return null;
                }

                var entry = GetOrCreateEntry(masterId);
                if (entry.HasGeneration && sessionGeneration < entry.LatestSessionGeneration)
                {
                    return null;
                }

                if (entry.CurrentOwner is { } current &&
                    (sessionGeneration < current.SessionGeneration ||
                     (sessionGeneration == current.SessionGeneration && current.ConnectionId != connectionId)))
                {
                    return null;
                }

                if (!entry.HasGeneration || sessionGeneration > entry.LatestSessionGeneration)
                {
                    entry.LatestSessionGeneration = sessionGeneration;
                    entry.HasGeneration = true;
                }

                entry.LatestObservationSequence = checked(++_observationSequence);

                if (entry.CurrentOwner is null)
                {
                    return null;
                }

                entry.CurrentOwner = null;
                _pendingObservations.Remove(masterId);
                return friend;
            }
        }

        private void RecordPendingObservation(
            uint masterId,
            long sessionGeneration,
            string connectionId,
            bool signedIn)
        {
            if (_entries.TryGetValue(masterId, out var entry) &&
                entry.HasGeneration &&
                sessionGeneration < entry.LatestSessionGeneration)
            {
                return;
            }

            if (_pendingObservations.TryGetValue(masterId, out var existing))
            {
                if (sessionGeneration < existing.SessionGeneration ||
                    (sessionGeneration == existing.SessionGeneration &&
                     !signedIn && existing.SignedIn && existing.ConnectionId != connectionId))
                {
                    return;
                }
            }

            _pendingObservations[masterId] = new PendingPresenceObservation(
                masterId,
                sessionGeneration,
                connectionId,
                signedIn,
                checked(++_observationSequence));
        }

        private void ApplyPendingObservation(PendingPresenceObservation pending)
        {
            var entry = GetOrCreateEntry(pending.MasterId);
            if (entry.HasGeneration && pending.SessionGeneration < entry.LatestSessionGeneration)
            {
                return;
            }

            entry.HasGeneration = true;
            entry.LatestSessionGeneration = pending.SessionGeneration;
            entry.CurrentOwner = pending.SignedIn
                ? new SessionIdentity(pending.SessionGeneration, pending.ConnectionId)
                : null;
            entry.LatestObservationSequence = pending.ObservationSequence;
        }

        private PresenceEntry GetOrCreateEntry(uint masterId)
        {
            if (_entries.TryGetValue(masterId, out var entry))
            {
                return entry;
            }

            entry = new PresenceEntry();
            _entries[masterId] = entry;
            return entry;
        }

        private sealed class PresenceEntry
        {
            public bool HasGeneration { get; set; }

            public long LatestSessionGeneration { get; set; }

            public SessionIdentity? CurrentOwner { get; set; }

            public long LatestObservationSequence { get; set; }
        }

        private sealed record PendingPresenceObservation(
            uint MasterId,
            long SessionGeneration,
            string ConnectionId,
            bool SignedIn,
            long ObservationSequence);

        private readonly record struct SessionIdentity(long SessionGeneration, string ConnectionId);
    }
}
