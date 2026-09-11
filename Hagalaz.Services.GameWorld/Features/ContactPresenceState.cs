using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    internal sealed class ContactPresenceState
    {
        private readonly object _gate = new();
        private readonly Dictionary<uint, SessionIdentity> _owners = new();

        public void ReplaceFriends(
            IContactList<Friend> friends,
            IEnumerable<Friend> replacement,
            IEnumerable<ContactPresenceOwner> owners)
        {
            lock (_gate)
            {
                friends.Set(replacement);
                _owners.Clear();
                foreach (var owner in owners)
                {
                    _owners[owner.MasterId] = new SessionIdentity(owner.SessionGeneration, owner.ConnectionId);
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
                    if (!_owners.TryGetValue(masterId, out var current)
                        || presenceOwner.SessionGeneration >= current.SessionGeneration)
                    {
                        _owners[masterId] = new SessionIdentity(
                            presenceOwner.SessionGeneration,
                            presenceOwner.ConnectionId);
                    }
                }
                else
                {
                    _owners.Remove(masterId);
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
                return removed | _owners.Remove(masterId);
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
                if (_owners.TryGetValue(masterId, out var current) &&
                    sessionGeneration <= current.SessionGeneration)
                {
                    return null;
                }

                var friend = friends.Get(masterId);
                if (friend is null)
                {
                    return null;
                }

                _owners[masterId] = new SessionIdentity(sessionGeneration, connectionId);
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
                if (!_owners.TryGetValue(masterId, out var current) ||
                    current.SessionGeneration != sessionGeneration ||
                    current.ConnectionId != connectionId)
                {
                    return null;
                }

                var friend = friends.Get(masterId);
                if (friend is null)
                {
                    return null;
                }

                _owners.Remove(masterId);
                return friend;
            }
        }

        private readonly record struct SessionIdentity(long SessionGeneration, string ConnectionId);
    }
}
