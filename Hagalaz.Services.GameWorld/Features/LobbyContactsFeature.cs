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
