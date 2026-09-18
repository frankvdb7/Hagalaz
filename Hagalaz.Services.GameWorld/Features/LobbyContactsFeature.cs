using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    public class LobbyContactsFeature : IContactsFeature
    {
        private readonly IContactList<Friend> _friends = new ContactList<Friend>();
        private readonly IContactList<Ignore> _ignores = new ContactList<Ignore>();
        private readonly ContactPresenceState _presence = new();

        public IContactList<Friend> Friends => _friends;

        public IContactList<Ignore> Ignores => _ignores;

        public long CaptureObservationBoundary() => _presence.CaptureObservationBoundary();

        public void ReplaceFriends(IEnumerable<Friend> friends, IEnumerable<ContactPresenceOwner> onlineOwners, long observationBoundary = 0) =>
            _presence.ReplaceFriends(Friends, friends, onlineOwners, observationBoundary);

        public Friend AddFriend(Friend friend, ContactPresenceOwner? onlineOwner, long observationBoundary = 0) =>
            _presence.AddFriend(Friends, friend, onlineOwner, observationBoundary);

        public bool RemoveFriend(uint masterId) => _presence.RemoveFriend(Friends, masterId);

        public Friend? TryApplySignIn(uint masterId, long sessionGeneration, string connectionId)
            => _presence.TrySignIn(masterId, sessionGeneration, connectionId, Friends);

        public Friend? TryApplySignOut(uint masterId, long sessionGeneration, string connectionId)
            => _presence.TrySignOut(masterId, sessionGeneration, connectionId, Friends);
    }
}
