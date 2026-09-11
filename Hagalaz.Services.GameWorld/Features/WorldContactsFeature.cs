using System.Collections.Generic;
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

        public void ReplaceFriends(IEnumerable<Friend> friends, IEnumerable<ContactPresenceOwner> onlineOwners) =>
            _presence.ReplaceFriends(Friends, friends, onlineOwners);

        public Friend AddFriend(Friend friend, ContactPresenceOwner? onlineOwner) =>
            _presence.AddFriend(Friends, friend, onlineOwner);

        public bool RemoveFriend(uint masterId) => _presence.RemoveFriend(Friends, masterId);

        public Friend? TryApplySignIn(uint masterId, long sessionGeneration, string connectionId)
            => _presence.TrySignIn(masterId, sessionGeneration, connectionId, Friends);

        public Friend? TryApplySignOut(uint masterId, long sessionGeneration, string connectionId)
            => _presence.TrySignOut(masterId, sessionGeneration, connectionId, Friends);
    }
}
