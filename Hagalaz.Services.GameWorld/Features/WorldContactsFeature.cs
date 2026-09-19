using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    public class WorldContactsFeature : IContactsFeature
    {
        private readonly ICharacter _character;
        private readonly ContactPresenceState _presence = new();

        public WorldContactsFeature(ICharacter character) => _character = character;

        public IContactList<Friend> Friends => _character.Friends;

        public IContactList<Ignore> Ignores => _character.Ignores;

        public long CaptureObservationBoundary() => _presence.CaptureObservationBoundary();

        public long BeginObservationWindow() => _presence.BeginObservationWindow();

        public void EndObservationWindow() => _presence.EndObservationWindow(Friends);

        public Task WaitForInitialSnapshotAsync(CancellationToken cancellationToken = default) =>
            _presence.WaitForInitialSnapshotAsync(cancellationToken);

        public void CompleteInitialSnapshot() => _presence.CompleteInitialSnapshot();

        public ContactPresenceView GetPresence(uint masterId) => _presence.GetPresence(masterId);

        public void ReplaceFriends(IEnumerable<Friend> friends, IEnumerable<ContactPresenceOwner> onlineOwners, long observationBoundary = 0) =>
            _presence.ReplaceFriends(Friends, friends, onlineOwners, observationBoundary);

        public Friend AddFriend(Friend friend, ContactPresenceOwner? onlineOwner, long observationBoundary = 0) =>
            _presence.AddFriend(Friends, friend, onlineOwner, observationBoundary);

        public bool RemoveFriend(uint masterId) => _presence.RemoveFriend(Friends, masterId);

        public Friend? TryApplySignIn(uint masterId, long sessionGeneration, string connectionId, int? worldId = null, string? worldName = null)
            => _presence.TrySignIn(masterId, sessionGeneration, connectionId, worldId, worldName, Friends);

        public Friend? TryApplySignOut(uint masterId, long sessionGeneration, string connectionId)
            => _presence.TrySignOut(masterId, sessionGeneration, connectionId, Friends);
    }
}
