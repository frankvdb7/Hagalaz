using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    internal sealed class ContactPresenceState
    {
        private readonly object _gate = new();
        private readonly Dictionary<uint, PresenceEntry> _entries = new();

        public void ReplaceFriends(
            IContactList<Friend> friends,
            IEnumerable<Friend> replacement,
            IEnumerable<ContactPresenceOwner> owners)
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
                    }
                }

                foreach (var entry in _entries)
                {
                    if (!seededOwners.Contains(entry.Key))
                    {
                        entry.Value.CurrentOwner = null;
                    }
                }
            }
        }

        public Friend AddFriend(
            IContactList<Friend> friends,
            Friend friend,
            ContactPresenceOwner? owner)
        {
            lock (_gate)
            {
                var masterId = unchecked((uint)friend.MasterId);
                friends.Remove(masterId);
                friends.Add(friend);

                if (owner is { } presenceOwner)
                {
                    var entry = GetOrCreateEntry(masterId);
                    if (!entry.HasGeneration || presenceOwner.SessionGeneration > entry.LatestSessionGeneration)
                    {
                        entry.LatestSessionGeneration = presenceOwner.SessionGeneration;
                        entry.HasGeneration = true;
                        entry.CurrentOwner = new SessionIdentity(
                            presenceOwner.SessionGeneration,
                            presenceOwner.ConnectionId);
                    }
                }
                else if (_entries.TryGetValue(masterId, out var entry))
                {
                    entry.CurrentOwner = null;
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
                    return null;
                }

                var entry = existingEntry ?? GetOrCreateEntry(masterId);
                entry.LatestSessionGeneration = sessionGeneration;
                entry.HasGeneration = true;
                entry.CurrentOwner = new SessionIdentity(sessionGeneration, connectionId);
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

                if (entry.CurrentOwner is null)
                {
                    return null;
                }

                var friend = friends.Get(masterId);
                if (friend is null)
                {
                    entry.CurrentOwner = null;
                    return null;
                }

                entry.CurrentOwner = null;
                return friend;
            }
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
        }

        private readonly record struct SessionIdentity(long SessionGeneration, string ConnectionId);
    }
}
