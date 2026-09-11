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
            _presence.ReplaceOwners(onlineOwners, () => Friends.Set(friends));

        public Friend? TryApplySignIn(uint masterId, long sessionGeneration, string connectionId)
        {
            Friend? friend = null;
            return _presence.TrySignIn(
                masterId,
                sessionGeneration,
                connectionId,
                () => (friend = Friends.Get(masterId)) is not null)
                ? friend
                : null;
        }

        public Friend? TryApplySignOut(uint masterId, long sessionGeneration, string connectionId)
        {
            Friend? friend = null;
            return _presence.TrySignOut(
                masterId,
                sessionGeneration,
                connectionId,
                () => (friend = Friends.Get(masterId)) is not null)
                ? friend
                : null;
        }
    }
}
